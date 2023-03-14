﻿using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.UI.Elements;
using Terraria.UI;
using TerrariaOverhaul.Core.Configuration;
using TerrariaOverhaul.Core.Interface;

namespace TerrariaOverhaul.Common.ConfigurationScreen;

public sealed class ConfigurationState : UIState
{
	// Search
	private string? searchString;
	// Etc.
	private bool clickedSearchBar;
	private bool clickedSomething;
	private bool inCategoryMenu = true;

	// Main
	public FancyUIPanel MainPanel { get; private set; } = null!;
	public UIElement ContentArea { get; private set; } = null!;
	public UITextPanel<LocalizedText> ExitButton { get; private set; } = null!;
	public UIElement ContentContainer { get; private set; } = null!;
	public UIElement PanelGridContainer { get; private set; } = null!;
	// Search
	public UIImageButton SearchButton { get; private set; } = null!;
	public UISearchBar SearchBar { get; private set; } = null!;
	public FancyUIPanel SearchBarPanel { get; private set; } = null!;

	public override void OnInitialize()
	{
		// Main Elements

		ContentArea = this.AddElement(new UIElement().With(e => {
			e.Width = StyleDimension.FromPixels(800f);
			e.Top = StyleDimension.FromPixels(220f);
			e.Height = StyleDimension.FromPixelsAndPercent(-220f, 1f);
			e.HAlign = 0.5f;
		}));

		ExitButton = ContentArea.AddElement(new UITextPanel<LocalizedText>(Language.GetText("UI.Back"), 0.7f, large: true).With(e => {
			e.Width = StyleDimension.FromPixelsAndPercent(-10f, 0.5f);
			e.Height = StyleDimension.FromPixels(50f);
			e.VAlign = 1f;
			e.HAlign = 0.5f;
			e.Top = StyleDimension.FromPixels(-25f);

			e.AddComponent(new DynamicColorsUIComponent {
				Border = CommonColors.OuterPanelMedium.Border,
				Background = CommonColors.OuterPanelMedium.Background,
			});

			e.AddComponent(new SoundPlaybackUIComponent {
				HoverSound = SoundID.MenuTick,
			});

			e.OnMouseDown += (_, _) => BackButtonLogic();

			e.SetSnapPoint("ExitButton", 0);
		}));

		MainPanel = ContentArea.AddElement(new FancyUIPanel().With(e => {
			e.Width = StyleDimension.Fill;
			e.Height = StyleDimension.FromPixelsAndPercent(-90f, 1f);

			e.Colors.Border = CommonColors.OuterPanelDark.Border;
			e.Colors.Background = CommonColors.OuterPanelDark.Background;

			e.SetPadding(0f);
		}));

		ContentContainer = MainPanel.AddElement(new UIElement().With(e => {
			e.Width = StyleDimension.Fill;
			e.Height = StyleDimension.FromPixelsAndPercent(-48f, 1f);
			e.Top = StyleDimension.FromPixels(48f);
			e.PaddingLeft = 15f;
			e.PaddingRight = 15f;
			e.PaddingBottom = 15f;
		}));

		// Search Bar

		var searchBarSection = MainPanel.AddElement(new UIElement().With(e => {
			e.Width = StyleDimension.Fill;
			e.Height = StyleDimension.FromPixels(24f);
			e.Top = StyleDimension.FromPixels(12f);
			e.VAlign = 0f;

			e.SetPadding(0f);
		}));

		SearchBarPanel = searchBarSection.AddElement(new FancyUIPanel().With(e => {
			e.Width = StyleDimension.FromPercent(0.95f);
			e.Height = StyleDimension.Fill;
			e.HAlign = 0.5f;
			e.VAlign = 0.5f;

			e.Colors.Border = CommonColors.InnerPanelBright.Border;
			e.Colors.Background = CommonColors.InnerPanelBright.Background;

			e.SetPadding(0f);
		}));

		SearchBar = SearchBarPanel.AddElement(new UISearchBar(Language.GetText("Search"), 0.8f).With(e => {
			e.Width = StyleDimension.Fill;
			e.Height = StyleDimension.Fill;
			e.HAlign = 0f;
			e.VAlign = 0.5f;

			e.OnClick += Click_SearchArea;
			e.OnContentsChanged += OnSearchContentsChanged;
			e.OnStartTakingInput += OnStartTakingInput;
			e.OnEndTakingInput += OnEndTakingInput;

			e.SetContents(null, forced: true);
		}));

		var searchCancelButton = SearchBar.AddElement(new UIImageButton(Main.Assets.Request<Texture2D>("Images/UI/SearchCancel")).With(e => {
			e.HAlign = 1f;
			e.VAlign = 0.5f;
			e.Left = StyleDimension.FromPixels(-2f);

			e.AddComponent(new SoundPlaybackUIComponent {
				HoverSound = SoundID.MenuTick,
			});

			e.OnClick += SearchCancelButton_OnClick;
		}));

		// Panel Grid

		PanelGridContainer = ContentContainer.AddElement(new UIElement().With(e => {
			e.Width = StyleDimension.Fill;
			e.Height = StyleDimension.Fill;
		}));

		var panelGrid = PanelGridContainer.AddElement(new UIGrid().With(e => {
			e.Width = StyleDimension.FromPixelsAndPercent(-20, 1f);
			e.Height = StyleDimension.Fill;
			e.ListPadding = 15f;
			e.PaddingRight = 15f;
		}));

		var panelGridScrollbar = PanelGridContainer.AddElement(new UIScrollbar().With(e => {
			e.HAlign = 1f;
			e.VAlign = 0.5f;
			e.Height = StyleDimension.FromPixelsAndPercent(-8f, 1f);

			panelGrid.SetScrollbar(e);
		}));

		string assetLocation = $"{nameof(TerrariaOverhaul)}/Assets/Textures/UI/Config";

		var thumbnailPlaceholder = ModContent.Request<Texture2D>($"{assetLocation}/NoPreview");
		var configCategories = ConfigSystem.CategoriesByName.Keys.OrderBy(s => s);

		foreach (string category in configCategories) {
			var localizedCategoryName = Language.GetText($"Mods.{nameof(TerrariaOverhaul)}.Configuration.{category}.DisplayName");

			string thumbnailPath = $"{assetLocation}/{category}/Category";
			string thumbnailVideoPath = $"{thumbnailPath}Video";

			CardPanel cardPanel;

			if (ModContent.HasAsset(thumbnailVideoPath)) {
				var thumbnailVideo = ModContent.Request<Video>(thumbnailVideoPath);

				cardPanel = new CardPanel(localizedCategoryName, thumbnailVideo);
			} else {
				var thumbnailTexture = ModContent.HasAsset(thumbnailPath) ? ModContent.Request<Texture2D>(thumbnailPath) : thumbnailPlaceholder;

				cardPanel = new CardPanel(localizedCategoryName, thumbnailTexture);
			}

			panelGrid.Add(cardPanel);

			cardPanel.OnClick += (_, _) => SwitchToCategorySettings(category);
		}
	}

	private void SwitchToCategorySettings(string category)
	{
		SoundEngine.PlaySound(in SoundID.MenuOpen);

		PanelGridContainer.Remove();

		ContentContainer.AddElement(new SettingsPanel().With(e => {
			var categoryData = ConfigSystem.CategoriesByName[category];

			foreach (var configEntry in categoryData.EntriesByName.Values) {
				e.AddOption(configEntry);
			}
		}));

		inCategoryMenu = false;
	}

	private void BackButtonLogic()
	{
		SoundEngine.PlaySound(SoundID.MenuClose);

		if (inCategoryMenu) {
			Main.menuMode = MenuID.Title;
		} else {
			ContentContainer.RemoveAllChildren();
			ContentContainer.Append(PanelGridContainer);

			inCategoryMenu = true;
		}
	}

	#region Search Bar Nonsense

	private void Click_SearchArea(UIMouseEvent evt, UIElement listeningElement)
	{
		if (SearchBar == null) {
			return;
		}

		if (listeningElement == SearchBar || listeningElement == SearchButton || listeningElement == SearchBarPanel) {
			SearchBar.ToggleTakingText();

			clickedSearchBar = true;
		}
	}

	private void OnSearchContentsChanged(string contents)
	{
		searchString = contents;
	}

	private void OnStartTakingInput()
	{
		SearchBarPanel.BorderColor = Main.OurFavoriteColor;
	}

	private void OnEndTakingInput()
	{
		SearchBarPanel.BorderColor = new Color(73, 94, 171);
	}

	private void OnFinishedSettingName(string name)
	{
		string contents = name.Trim();

		SearchBar.SetContents(contents);
		GoBackHere();
	}

	private void GoBackHere()
	{
		UserInterface.ActiveInstance.SetState(this);

		if (SearchBar.IsWritingText) {
			SearchBar.ToggleTakingText();
		}
	}

	private void SearchCancelButton_OnClick(UIMouseEvent evt, UIElement listeningElement)
	{
		if (SearchBar.HasContents) {
			SearchBar.SetContents(null, forced: true);
			SoundEngine.PlaySound(SoundID.MenuClose);
		} else {
			SoundEngine.PlaySound(SoundID.MenuTick);
		}

		GoBackHere();
	}

	public override void Click(UIMouseEvent evt)
	{
		base.Click(evt);

		clickedSomething = true;
	}

	#endregion

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);

		if (clickedSomething && !clickedSearchBar && SearchBar.IsWritingText) {
			SearchBar.ToggleTakingText();
		}

		if (Main.keyState.IsKeyDown(Keys.Escape) && !Main.oldKeyState.IsKeyDown(Keys.Escape)) {
			if (SearchBar.IsWritingText) {
				GoBackHere();
			} else {
				BackButtonLogic();
			}
		}
	}
}
