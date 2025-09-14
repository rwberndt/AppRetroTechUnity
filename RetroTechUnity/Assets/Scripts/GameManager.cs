using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using System.Linq;
#if ZXING_PRESENT
using ZXing;
using ZXing.Common;
#endif

using static RetroTech.UiKit;

namespace RetroTech
{
    public class GameManager : MonoBehaviour
    {
        private TextMeshProUGUI _scoreLabel;
        private Image _progressFill;
        private GameObject _nextBtnGO;
        private TextMeshProUGUI _expTextTMP;

        private Canvas _canvas;
        private GameObject[] _pages;
        private GameObject _navBar;

        // Updated Palette to match prototype
        private readonly Color BackgroundColor = new Color32(23, 22, 35, 255);
        private readonly Color CardColor = new Color32(255, 255, 255, 30); // Glass effect
        private readonly Color PrimaryColor = new Color32(114, 74, 160, 255);
        private readonly Color AccentColor = new Color32(255, 255, 255, 255);

        // Beautiful gradient colors from prototype
        private readonly Color GradientTop = new Color32(147, 112, 219, 255);    // Light purple
        private readonly Color GradientBottom = new Color32(255, 182, 193, 255); // Light pink
        private Sprite _fallbackGradient;

        // Icons (Resources/Icons/*.png)
        private Sprite _iconHome, _iconCategories, _iconTimeline, _iconScanner, _iconQuiz;

        private Dictionary<string, bool> _categoryExpanded = new();
        private int _currentQuizIndex;
        private int _quizScore;
        private int _activeTab = 0;
        private GameObject _openModal;

        private HomePage _homePage;

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            var go = new GameObject("GameManager");
            DontDestroyOnLoad(go);
            go.AddComponent<GameManager>();
        }

        private void Awake()
        {
            _fallbackGradient = CreateFallbackGradient(GradientTop, GradientBottom);
            LoadIcons();
            CreateCanvas();
            SetupBackground();

            CreatePages();
            CreateNavigationBar();
            SwitchPage(0);
        }

        // ========= Canvas & Input =========

        private void CreateCanvas()
        {
            var canvasGO = new GameObject("Canvas");
            canvasGO.layer = LayerMask.NameToLayer("UI");
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
                es.AddComponent<TouchInputModule>();
#endif
            }
        }

        private void SetupBackground()
        {
            // Create fullscreen background with gradient
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(_canvas.transform, false);

            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            var bgImg = bgGO.AddComponent<Image>();
            bgImg.sprite = _fallbackGradient;
            bgImg.type = Image.Type.Simple;
            bgImg.color = Color.white;

            bgGO.transform.SetAsFirstSibling();
        }

        private void LoadIcons()
        {
            _iconHome = Resources.Load<Sprite>("Icons/icon_home");
            _iconCategories = Resources.Load<Sprite>("Icons/icon_categories");
            _iconTimeline = Resources.Load<Sprite>("Icons/icon_timeline");
            _iconScanner = Resources.Load<Sprite>("Icons/icon_scanner");
            _iconQuiz = Resources.Load<Sprite>("Icons/icon_quiz");
        }

        private Sprite CreateFallbackGradient(Color top, Color bottom)
        {
            Texture2D tex = new Texture2D(1, 64);
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[64];
            for (int i = 0; i < 64; i++)
            {
                float t = i / 63f;
                colors[i] = Color.Lerp(bottom, top, t);
            }

            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
        }

        // ========= Page factory =========

        private void CreatePages()
        {
            _pages = new GameObject[5];
            _pages[0] = CreateHomePage();
            _pages[1] = CreateCategoriesPage();
            _pages[2] = CreateTimelinePage();
            _pages[3] = CreateScannerPage();
            _pages[4] = CreateQuizPage();
        }

        /// <summary>Creates a glass-style content container that fills the screen above navigation.</summary>
        private (GameObject surface, RectTransform content) BuildPrototypeSurface(string name)
        {
            var surface = new GameObject(name);
            surface.transform.SetParent(_canvas.transform, false);

            var srt = surface.AddComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 0.08f); // Leave space for nav bar
            srt.anchorMax = new Vector2(1f, 1f);
            srt.offsetMin = new Vector2(16, 16); // Small margin
            srt.offsetMax = new Vector2(-16, -16);

            // Scrollable content container
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(surface.transform, false);

            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = Vector2.zero;

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.vertical = true;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;

            // Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRT = viewport.GetComponent<RectTransform>();
            vpRT.anchorMin = Vector2.zero;
            vpRT.anchorMax = Vector2.one;
            vpRT.offsetMin = Vector2.zero;
            vpRT.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            scroll.viewport = vpRT;

            // Content
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var crt = content.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0, 1);
            crt.anchorMax = new Vector2(1, 1);
            crt.pivot = new Vector2(0.5f, 1);
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            scroll.content = crt;

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(40, 40, 60, 60); // Much more padding
            vlg.spacing = 30; // More spacing between elements
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return (surface, crt);
        }

        // ========= Navigation =========
        private const float NavBarHeight = 80f;

        private void CreateNavigationBar()
        {
            if (_navBar != null) Destroy(_navBar);

            _navBar = new GameObject("NavigationBar", typeof(RectTransform));
            _navBar.transform.SetParent(_canvas.transform, false);
            var rt = _navBar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;

            float bottomInset = Screen.safeArea.y;
            rt.sizeDelta = new Vector2(0f, NavBarHeight + bottomInset);

            // Glass background effect
            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(_navBar.transform, false);
            var bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;

            var bgImg = bgGO.GetComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.3f); // Dark glass effect

            // Navigation icons row
            var row = new GameObject("IconsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            row.transform.SetParent(bgGO.transform, false);
            var rowRT = row.GetComponent<RectTransform>();
            rowRT.anchorMin = Vector2.zero;
            rowRT.anchorMax = Vector2.one;
            rowRT.offsetMin = new Vector2(16, 12 + bottomInset);
            rowRT.offsetMax = new Vector2(-16, -12);

            var hlg = row.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 8f;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;

            // Create navigation tabs matching prototype
            CreatePrototypeNavTab(row.transform, "Início", 0, _iconHome);
            CreatePrototypeNavTab(row.transform, "Categorias", 1, _iconCategories);
            CreatePrototypeNavTab(row.transform, "Timeline", 2, _iconTimeline);
            CreatePrototypeNavTab(row.transform, "QR Code", 3, _iconScanner);
            CreatePrototypeNavTab(row.transform, "Quiz", 4, _iconQuiz);

            _navBar.transform.SetAsLastSibling();
            RefreshTabsVisual();
        }

        private void CreatePrototypeNavTab(Transform parent, string label, int pageIndex, Sprite icon)
        {
            var tab = new GameObject($"Tab_{label}", typeof(RectTransform), typeof(LayoutElement), typeof(Image), typeof(Button));
            tab.transform.SetParent(parent, false);

            var le = tab.GetComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 56;

            var bg = tab.GetComponent<Image>();
            bg.color = new Color(1, 1, 1, 0);

            var btn = tab.GetComponent<Button>();
            btn.targetGraphic = bg;
            btn.transition = Selectable.Transition.ColorTint;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1, 1, 1, 0.1f);
            colors.pressedColor = new Color(1, 1, 1, 0.2f);
            btn.colors = colors;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                _activeTab = pageIndex;
                SwitchPage(pageIndex);
                RefreshTabsVisual();
            });

            // Layout for icon + text
            var vlg = tab.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;

            // Icon
            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(tab.transform, false);
            var iconImg = iconGO.GetComponent<Image>();
            iconImg.raycastTarget = false;
            iconImg.preserveAspect = true;
            iconImg.color = Color.white;

            if (icon != null)
                iconImg.sprite = icon;
            else
            {
                // Fallback icon
                var tex = new Texture2D(2, 2);
                tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                tex.Apply();
                iconImg.sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 100);
            }

            var iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.sizeDelta = new Vector2(24, 24);
            var iconLE = iconGO.AddComponent<LayoutElement>();
            iconLE.preferredWidth = 24;
            iconLE.preferredHeight = 24;

            // Label using TMP
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(tab.transform, false);
            var labelTMP = UiKit.TMP(labelGO.transform, label, 11, new Color32(210, 210, 235, 255), TextAlignmentOptions.Center);
            labelTMP.enableWordWrapping = false;
            labelTMP.overflowMode = TMPro.TextOverflowModes.Ellipsis;

            var labelRT = labelTMP.GetComponent<RectTransform>();
            labelRT.sizeDelta = new Vector2(0, 14);
            var labelLE = labelGO.AddComponent<LayoutElement>();
            labelLE.preferredHeight = 14;
        }

        private void RefreshTabsVisual()
        {
            var row = _navBar.transform.Find("Background/IconsRow");
            if (!row) return;

            for (int i = 0; i < row.childCount; i++)
            {
                var tab = row.GetChild(i);
                bool active = (i == _activeTab);

                // Icon color
                var icon = tab.Find("Icon").GetComponent<Image>();
                icon.color = active ? Color.white : new Color32(180, 180, 200, 200);

                // Label color
                var label = tab.Find("Label").GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.color = active ? Color.white : new Color32(180, 180, 200, 200);
            }
        }

        public void SwitchPage(int index)
        {
            for (int i = 0; i < _pages.Length; i++)
                if (_pages[i] != null) _pages[i].SetActive(i == index);

            _activeTab = index;
            RefreshTabsVisual();
        }

        // ========= Pages =========

        private GameObject CreateHomePage()
        {
            // Criar o GameObject que irá conter a HomePage
            var homePageContainer = new GameObject("HomePageContainer");

            // Adicionar o componente HomePage
            _homePage = homePageContainer.AddComponent<HomePage>();

            // Configurar o evento de clique do botão "Buscar Peças"
            _homePage.OnSearchPiecesClicked += () =>
            {
                // Navegar para a página de categorias
                _activeTab = 1;
                SwitchPage(_activeTab);
                RefreshTabsVisual();
            };

            // Criar a página usando a nova classe
            var pageObject = _homePage.CreatePage(_canvas.transform, BuildPrototypeSurface);

            return pageObject;
        }

        /// <summary>
        /// Método auxiliar para acessar a página inicial
        /// </summary>
        public HomePage GetHomePage()
        {
            return _homePage;
        }

        /// <summary>
        /// Atualiza configurações da página inicial (exemplo de uso)
        /// </summary>
        public void UpdateHomePageSettings(float titleSize, float welcomeSize, float descSize)
        {
            if (_homePage != null)
            {
                _homePage.UpdateFontSizes(titleSize, welcomeSize, descSize);
            }
        }


        private CategoriesPage _categoriesPage;

        // ... outros métodos existentes ...

        /// <summary>
        /// Cria a página de categorias usando a classe CategoriesPage separada
        /// </summary>
        private GameObject CreateCategoriesPage()
        {
            // Criar o GameObject que irá conter a CategoriesPage
            var categoriesPageContainer = new GameObject("CategoriesPageContainer");

            // Adicionar o componente CategoriesPage
            _categoriesPage = categoriesPageContainer.AddComponent<CategoriesPage>();

            // Configurar o evento de seleção de peça
            _categoriesPage.OnPieceSelected += (piece) =>
            {
                // Mostrar detalhes da peça selecionada
                ShowPieceDetail(piece);
            };

            // Criar a página usando a nova classe
            var pageObject = _categoriesPage.CreatePage(_canvas.transform, _canvas, BuildPrototypeSurface);

            return pageObject;
        }

        /// <summary>
        /// Método auxiliar para acessar a página de categorias
        /// </summary>
        public CategoriesPage GetCategoriesPage()
        {
            return _categoriesPage;
        }

        /// <summary>
        /// Atualiza configurações da página de categorias (exemplo de uso)
        /// </summary>
        public void UpdateCategoriesPageSettings(float headerHeight, float itemHeight, int titleSize, int itemSize)
        {
            if (_categoriesPage != null)
            {
                _categoriesPage.UpdateVisualSettings(headerHeight, itemHeight, titleSize, itemSize);
            }
        }

        /// <summary>
        /// Expande ou recolhe uma categoria específica programaticamente
        /// </summary>
        /// <param name="categoryId">ID da categoria</param>
        /// <param name="expand">True para expandir, false para recolher</param>
        public void SetCategoryExpanded(string categoryId, bool expand)
        {
            if (_categoriesPage != null)
            {
                _categoriesPage.SetCategoryExpanded(categoryId, expand);
            }
        }

        /// <summary>
        /// Fecha qualquer modal aberto na página de categorias
        /// </summary>
        public void CloseCategoriesModal()
        {
            if (_categoriesPage != null)
            {
                _categoriesPage.CloseModal();
            }
        }

        // IMPORTANTE: Remover ou comentar os métodos antigos relacionados a modal
        // pois agora eles estão na CategoriesPage:


        private GameObject CreateTimelinePage()
        {
            var (surface, content) = BuildPrototypeSurface("TimelinePage");

            // Timeline title
            var titleTMP = UiKit.TMP(content, "Linha do Tempo", 36, Color.white, TextAlignmentOptions.Left, bold: true);
            titleTMP.margin = new Vector4(0, 0, 0, 24);

            var pieces = new List<ComputerPiece>(SampleData.Pieces);
            pieces.Sort((a, b) => a.YearManufactured.CompareTo(b.YearManufactured));

            foreach (var piece in pieces)
            {
                var timelineCard = CreateTimelineCard(content, piece);
            }

            return surface;
        }

        private GameObject CreateScannerPage()
        {
            var (surface, content) = BuildPrototypeSurface("ScannerPage");

            // Title
            var titleTMP = UiKit.TMP(content, "Scanner QR", 36, Color.white, TextAlignmentOptions.Left, bold: true);
            titleTMP.margin = new Vector4(0, 0, 0, 32);

            // QR Code preview area
            var previewCard = CreateGlassCard(content, 280f);
            var previewVLG = previewCard.gameObject.AddComponent<VerticalLayoutGroup>();
            previewVLG.childAlignment = TextAnchor.MiddleCenter;
            previewVLG.spacing = 16;
            previewVLG.padding = new RectOffset(24, 24, 24, 24);

            // QR Code icon placeholder
            var qrFrame = CreateQRFrame(previewCard.transform, 180f);

            var scanTitleTMP = UiKit.TMP(previewCard.transform, "Scanner QR Code", 24, Color.white, TextAlignmentOptions.Center, bold: true);
            scanTitleTMP.enableWordWrapping = false;

            var scanDescTMP = UiKit.TMP(previewCard.transform,
                "Aponte a câmera para o QR code de uma peça para ver seus detalhes.",
                18, new Color32(255, 255, 255, 180), TextAlignmentOptions.Center);
            scanDescTMP.margin = new Vector4(8, 0, 8, 0);

            AddSpacer(content, 24);

            // Start scanner button
            var scanButton = CreatePrototypeCTAButton(content.transform, "⚡ Iniciar Scanner", () =>
            {
                SimulateScan();
            });

            AddSpacer(content, 24);

            // Tips card
            var tipsCard = CreateGlassCard(content, 140f);
            var tipsVLG = tipsCard.gameObject.AddComponent<VerticalLayoutGroup>();
            tipsVLG.childAlignment = TextAnchor.UpperLeft;
            tipsVLG.spacing = 8;
            tipsVLG.padding = new RectOffset(20, 20, 16, 16);

            var tipsTitleTMP = UiKit.TMP(tipsCard.transform, "Dicas para escanear:", 20, Color.white, bold: true);
            tipsTitleTMP.enableWordWrapping = false;

            var tipsTMP = UiKit.TMP(tipsCard.transform,
                "• Mantenha o QR code bem iluminado\n" +
                "• Mantenha a câmera estável\n" +
                "• Certifique-se que o código esteja completo na tela",
                16, new Color32(255, 255, 255, 180), TextAlignmentOptions.Left);

            return surface;
        }

        private GameObject CreateQuizPage()
        {
            var (surface, content) = BuildPrototypeSurface("QuizPage");

            // Quiz header with progress
            var headerCard = CreateGlassCard(content, 60f);
            var headerHLG = headerCard.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerHLG.padding = new RectOffset(20, 20, 12, 12);
            headerHLG.spacing = 16;
            headerHLG.childAlignment = TextAnchor.MiddleCenter;
            headerHLG.childForceExpandWidth = false;

            var quizTitleTMP = UiKit.TMP(headerCard.transform, "Quiz RetroTech", 28, Color.white, TextAlignmentOptions.Left, bold: true);
            quizTitleTMP.enableWordWrapping = false;

            var spacerGO = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacerGO.transform.SetParent(headerCard.transform, false);
            spacerGO.GetComponent<LayoutElement>().flexibleWidth = 1;

            _scoreLabel = (TextMeshProUGUI)UiKit.TMP(headerCard.transform, "Pontuação: 0", 20, Color.white, TextAlignmentOptions.Right);
            _scoreLabel.enableWordWrapping = false;

            // Progress bar
            var progressCard = CreateProgressBar(content);

            // Question card
            var questionCard = CreateGlassCard(content, 120f);
            var questionVLG = questionCard.gameObject.AddComponent<VerticalLayoutGroup>();
            questionVLG.padding = new RectOffset(20, 20, 16, 16);
            questionVLG.spacing = 0;

            var qText = UiKit.TMP(questionCard.transform, "", 20, Color.white, TextAlignmentOptions.TopLeft);
            qText.enableWordWrapping = true;

            // Options container
            var optionsContainer = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup));
            optionsContainer.transform.SetParent(content, false);
            var optionsVLG = optionsContainer.GetComponent<VerticalLayoutGroup>();
            optionsVLG.spacing = 12;
            optionsVLG.padding = new RectOffset(0, 0, 8, 8);
            optionsVLG.childControlHeight = true;
            optionsVLG.childForceExpandHeight = false;
            optionsVLG.childControlWidth = true;
            optionsVLG.childForceExpandWidth = true;

            // Explanation card
            var expCard = CreateGlassCard(content, 100f);
            var expVLG = expCard.gameObject.AddComponent<VerticalLayoutGroup>();
            expVLG.padding = new RectOffset(16, 16, 12, 12);
            expVLG.spacing = 8;

            UiKit.TMP(expCard.transform, "❌ Incorreto!", 16, Color.white, TextAlignmentOptions.Left, bold: true)
                .name = "ExpTitle";

            _expTextTMP = (TextMeshProUGUI)UiKit.TMP(expCard.transform, "", 16, new Color32(255, 255, 255, 200), TextAlignmentOptions.Left);
            _expTextTMP.enableWordWrapping = true;

            expCard.gameObject.SetActive(false);

            // Next button
            _nextBtnGO = CreatePrototypeCTAButton(content.transform, "Próxima Pergunta", null);
            _nextBtnGO.SetActive(false);

            // Set up the next button click after it's created
            var nextButton = _nextBtnGO.GetComponent<Button>();
            nextButton.onClick.AddListener(() =>
            {
                _currentQuizIndex++;
                if (_currentQuizIndex >= SampleData.QuizQuestions.Count)
                    ShowQuizResult(content.gameObject);
                else
                    PopulateQuizQuestion((TextMeshProUGUI)qText, optionsContainer, expCard.gameObject, _nextBtnGO);
            });

            // Initialize quiz
            _currentQuizIndex = 0;
            _quizScore = 0;
            PopulateQuizQuestion((TextMeshProUGUI)qText, optionsContainer, expCard.gameObject, _nextBtnGO);

            return surface;
        }

        // ========= Helper Methods for Prototype UI =========

        private Image CreateGlassCard(Transform parent, float height)
        {
            var cardGO = new GameObject("GlassCard", typeof(RectTransform), typeof(Image));
            cardGO.transform.SetParent(parent, false);

            var cardRT = cardGO.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0, 1);
            cardRT.anchorMax = new Vector2(1, 1);
            cardRT.pivot = new Vector2(0.5f, 1);
            cardRT.offsetMin = Vector2.zero;
            cardRT.offsetMax = Vector2.zero;

            var cardLE = cardGO.AddComponent<LayoutElement>();
            cardLE.preferredHeight = height;
            cardLE.minHeight = height;

            var cardImg = cardGO.GetComponent<Image>();
            cardImg.color = new Color(1f, 1f, 1f, 0.1f); // Glass effect
            cardImg.raycastTarget = true;

            // Add rounded corners if you have a rounded sprite
            var roundedSprite = Resources.Load<Sprite>("Sprites/RoundedPanel");
            if (roundedSprite != null)
            {
                cardImg.sprite = roundedSprite;
                cardImg.type = Image.Type.Sliced;
            }

            return cardImg;
        }

        private GameObject CreatePrototypeCTAButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var btnCard = CreateGlassCard(parent, 48f);
            btnCard.color = Color.white; // White button like prototype

            var btn = btnCard.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnCard;
            btn.onClick.AddListener(onClick);

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            btn.colors = colors;

            var label = UiKit.TMP(btnCard.transform, text, 18, new Color32(103, 80, 164, 255), TextAlignmentOptions.Center, bold: true);
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(16, 8);
            lrt.offsetMax = new Vector2(-16, -8);

            return btnCard.gameObject;
        }

        private GameObject CreateLargerCTAButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var btnCard = CreateGlassCard(parent, 70f); // Taller button
            btnCard.color = Color.white;

            var btn = btnCard.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnCard;
            btn.onClick.AddListener(onClick);

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            btn.colors = colors;

            var label = UiKit.TMP(btnCard.transform, text, 28, new Color32(103, 80, 164, 255), TextAlignmentOptions.Center, bold: true);
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            var lrt = label.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(16, 12);
            lrt.offsetMax = new Vector2(-16, -12);

            return btnCard.gameObject;
        }


        private (GameObject header, RectTransform chevron) CreatePrototypeCategoryHeader(Transform parent, string title)
        {
            var headerCard = CreateGlassCard(parent, 56f);
            headerCard.color = new Color(1f, 1f, 1f, 0.15f); // Slightly more opaque for headers

            var btn = headerCard.gameObject.AddComponent<Button>();
            btn.targetGraphic = headerCard;

            var colors = btn.colors;
            colors.highlightedColor = new Color(1, 1, 1, 0.2f);
            colors.pressedColor = new Color(1, 1, 1, 0.25f);
            btn.colors = colors;

            var titleTMP = UiKit.TMP(headerCard.transform, title, 20, Color.white, TextAlignmentOptions.MidlineLeft, bold: false);
            titleTMP.enableWordWrapping = false;
            titleTMP.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            titleTMP.rectTransform.anchorMin = Vector2.zero;
            titleTMP.rectTransform.anchorMax = Vector2.one;
            titleTMP.rectTransform.offsetMin = new Vector2(20, 8);
            titleTMP.rectTransform.offsetMax = new Vector2(-44, -8);
            titleTMP.raycastTarget = false;

            var chevron = CreateChevronIcon(headerCard.transform);
            return (headerCard.gameObject, chevron);
        }

        private RectTransform CreateChevronIcon(Transform parent)
        {
            var chevronGO = new GameObject("Chevron", typeof(RectTransform), typeof(Image));
            chevronGO.transform.SetParent(parent, false);
            var chevronRT = chevronGO.GetComponent<RectTransform>();
            chevronRT.anchorMin = new Vector2(1, 0.5f);
            chevronRT.anchorMax = new Vector2(1, 0.5f);
            chevronRT.pivot = new Vector2(1, 0.5f);
            chevronRT.sizeDelta = new Vector2(20, 20);
            chevronRT.anchoredPosition = new Vector2(-16, 0);

            var chevronImg = chevronGO.GetComponent<Image>();
            chevronImg.raycastTarget = false;

            // Try to load chevron sprite, fallback to text
            var chevronSprite = Resources.Load<Sprite>("Sprites/Chevron");
            if (chevronSprite != null)
            {
                chevronImg.sprite = chevronSprite;
                chevronImg.color = Color.white;
            }
            else
            {
                // Use text as fallback
                chevronGO.GetComponent<Image>().color = new Color(0, 0, 0, 0);
                var textTMP = UiKit.TMP(chevronGO.transform, "▼", 16, Color.white, TextAlignmentOptions.Center);
                textTMP.raycastTarget = false;
                var textRT = textTMP.rectTransform;
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = Vector2.zero;
                textRT.offsetMax = Vector2.zero;
            }

            return chevronRT;
        }

        private GameObject CreateTimelineCard(Transform parent, ComputerPiece piece)
        {
            var timelineCard = CreateGlassCard(parent, 200f);
            timelineCard.color = new Color(0f, 0f, 0f, 0.3f); // Dark card like prototype

            var cardVLG = timelineCard.gameObject.AddComponent<VerticalLayoutGroup>();
            cardVLG.padding = new RectOffset(20, 20, 16, 16);
            cardVLG.spacing = 12;
            cardVLG.childAlignment = TextAnchor.UpperLeft;
            cardVLG.childControlWidth = true;
            cardVLG.childForceExpandWidth = true;
            cardVLG.childControlHeight = false;
            cardVLG.childForceExpandHeight = false;

            // Year badge
            var yearTMP = UiKit.TMP(timelineCard.transform, piece.YearManufactured.ToString(), 16, new Color32(147, 112, 219, 255), TextAlignmentOptions.Left, bold: true);
            yearTMP.margin = new Vector4(0, 0, 0, 4);

            // Piece name
            var nameTMP = UiKit.TMP(timelineCard.transform, piece.Name, 22, Color.white, TextAlignmentOptions.Left, bold: true);
            nameTMP.enableWordWrapping = true;
            nameTMP.margin = new Vector4(0, 0, 0, 8);

            // Description
            var descTMP = UiKit.TMP(timelineCard.transform, $"{piece.Manufacturer} — {piece.Description}", 16, new Color32(255, 255, 255, 180), TextAlignmentOptions.Left);
            descTMP.enableWordWrapping = true;
            descTMP.margin = new Vector4(0, 0, 0, 16);

            // Details button
            var detailsBtn = CreatePrototypeCTAButton(timelineCard.transform, "Ver detalhes →", () => ShowPieceDetail(piece));
            var detailsBtnLE = detailsBtn.GetComponent<LayoutElement>();
            detailsBtnLE.preferredHeight = 40;
            detailsBtnLE.minHeight = 40;

            return timelineCard.gameObject;
        }

        private GameObject CreateQRFrame(Transform parent, float size)
        {
            var frameGO = new GameObject("QRFrame", typeof(RectTransform), typeof(Image));
            frameGO.transform.SetParent(parent, false);

            var frameRT = frameGO.GetComponent<RectTransform>();
            frameRT.sizeDelta = new Vector2(size, size);

            var frameImg = frameGO.GetComponent<Image>();
            frameImg.color = new Color(1f, 1f, 1f, 0.2f);
            frameImg.raycastTarget = false;

            // Try to load rounded frame sprite
            var frameSprite = Resources.Load<Sprite>("Sprites/RoundedPanel");
            if (frameSprite != null)
            {
                frameImg.sprite = frameSprite;
                frameImg.type = Image.Type.Sliced;
                frameImg.fillCenter = false; // Only border
            }

            var frameLE = frameGO.AddComponent<LayoutElement>();
            frameLE.preferredWidth = size;
            frameLE.preferredHeight = size;

            // QR Code icon inside
            var qrIconGO = new GameObject("QRIcon", typeof(RectTransform), typeof(Image));
            qrIconGO.transform.SetParent(frameGO.transform, false);

            var qrIconRT = qrIconGO.GetComponent<RectTransform>();
            qrIconRT.anchorMin = new Vector2(0.5f, 0.5f);
            qrIconRT.anchorMax = new Vector2(0.5f, 0.5f);
            qrIconRT.pivot = new Vector2(0.5f, 0.5f);
            qrIconRT.sizeDelta = new Vector2(80, 80);

            var qrIconImg = qrIconGO.GetComponent<Image>();
            qrIconImg.raycastTarget = false;

            // Try to load QR icon sprite, fallback to text
            var qrSprite = Resources.Load<Sprite>("Sprites/qr_glyph");
            if (qrSprite != null)
            {
                qrIconImg.sprite = qrSprite;
                qrIconImg.color = Color.white;
            }
            else
            {
                // Text fallback
                qrIconImg.color = new Color(0, 0, 0, 0);
                var qrTextTMP = UiKit.TMP(qrIconGO.transform, "⊞", 48, Color.white, TextAlignmentOptions.Center);
                var qrTextRT = qrTextTMP.rectTransform;
                qrTextRT.anchorMin = Vector2.zero;
                qrTextRT.anchorMax = Vector2.one;
                qrTextRT.offsetMin = Vector2.zero;
                qrTextRT.offsetMax = Vector2.zero;
            }

            return frameGO;
        }

        private GameObject CreateProgressBar(Transform parent)
        {
            var progressCard = CreateGlassCard(parent, 12f);
            progressCard.color = new Color(1f, 1f, 1f, 0.2f);

            var fillGO = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(progressCard.transform, false);

            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0, 1);
            fillRT.offsetMin = new Vector2(2, 2);
            fillRT.offsetMax = new Vector2(2, -2);

            _progressFill = fillGO.GetComponent<Image>();
            _progressFill.color = Color.white;
            _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal;
            _progressFill.fillAmount = 0f;

            return progressCard.gameObject;
        }

        // ========= Quiz Logic =========

        private void PopulateQuizQuestion(TextMeshProUGUI questionTMP, GameObject optionsContainer, GameObject explanationGO, GameObject nextGO)
        {
            foreach (Transform child in optionsContainer.transform)
                Destroy(child.gameObject);

            explanationGO.SetActive(false);
            nextGO.SetActive(false);

            var q = SampleData.QuizQuestions[_currentQuizIndex];

            _scoreLabel.text = $"Pontuação: {_quizScore}";
            _progressFill.fillAmount = (_currentQuizIndex) / Mathf.Max(1f, (float)(SampleData.QuizQuestions.Count - 1));

            questionTMP.text = q.Question;

            for (int i = 0; i < q.Options.Count; i++)
            {
                int idx = i;

                var optionCard = CreateGlassCard(optionsContainer.transform, 48f);
                optionCard.color = Color.white; // White options like prototype

                var btn = optionCard.gameObject.AddComponent<Button>();
                btn.targetGraphic = optionCard;

                var optionTMP = UiKit.TMP(optionCard.transform, q.Options[i], 16, new Color32(103, 80, 164, 255), TextAlignmentOptions.Center, bold: true);
                optionTMP.enableWordWrapping = false;
                optionTMP.raycastTarget = false;

                var optionRT = optionTMP.rectTransform;
                optionRT.anchorMin = Vector2.zero;
                optionRT.anchorMax = Vector2.one;
                optionRT.offsetMin = new Vector2(16, 8);
                optionRT.offsetMax = new Vector2(-16, -8);

                btn.onClick.AddListener(() =>
                {
                    bool correct = idx == q.CorrectAnswerIndex;
                    if (correct) _quizScore++;

                    var expTitle = explanationGO.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                    expTitle.text = correct ? "✅ Correto!" : "❌ Incorreto!";

                    _expTextTMP.text = q.Explanation;
                    explanationGO.SetActive(true);

                    // Update option colors
                    for (int c = 0; c < optionsContainer.transform.childCount; c++)
                    {
                        var opt = optionsContainer.transform.GetChild(c);
                        var img = opt.GetComponent<Image>();
                        var txt = opt.GetComponentInChildren<TextMeshProUGUI>();
                        var button = opt.GetComponent<Button>();
                        if (button) button.interactable = false;

                        if (c == q.CorrectAnswerIndex)
                        {
                            img.color = new Color32(76, 175, 80, 255); // Green for correct
                            txt.color = Color.white;
                        }
                        else if (c == idx && !correct)
                        {
                            img.color = new Color32(244, 67, 54, 255); // Red for wrong choice
                            txt.color = Color.white;
                        }
                        else
                        {
                            img.color = new Color32(200, 200, 200, 255); // Gray for others
                            txt.color = new Color32(100, 100, 100, 255);
                        }
                    }

                    _scoreLabel.text = $"Pontuação: {_quizScore}";
                    nextGO.SetActive(true);
                });
            }
        }

        private void ShowQuizResult(GameObject quizPageContent)
        {
            foreach (Transform c in quizPageContent.transform)
                Destroy(c.gameObject);

            var resultCard = CreateGlassCard(quizPageContent.transform, 200f);
            var resultVLG = resultCard.gameObject.AddComponent<VerticalLayoutGroup>();
            resultVLG.padding = new RectOffset(24, 24, 24, 24);
            resultVLG.spacing = 16;
            resultVLG.childAlignment = TextAnchor.MiddleCenter;

            var scoreTMP = UiKit.TMP(resultCard.transform,
                $"Você acertou {_quizScore} de {SampleData.QuizQuestions.Count} perguntas!",
                24, Color.white, TextAlignmentOptions.Center, bold: true);

            var msg = (_quizScore == SampleData.QuizQuestions.Count)
                ? "Excelente! Você é um expert em tecnologia retrô."
                : (_quizScore >= SampleData.QuizQuestions.Count / 2)
                    ? "Muito bom! Continue explorando para aprender mais."
                    : "Você pode melhorar! Que tal estudar mais sobre as peças?";

            var msgTMP = UiKit.TMP(resultCard.transform, msg, 18, new Color32(255, 255, 255, 200), TextAlignmentOptions.Center);

            // Save high score
            int previousHigh = PlayerPrefs.GetInt("RetroTech_HighScore", 0);
            if (_quizScore > previousHigh)
            {
                PlayerPrefs.SetInt("RetroTech_HighScore", _quizScore);
                PlayerPrefs.Save();
            }

            // Restart button
            CreatePrototypeCTAButton(resultCard.transform, "Reiniciar Quiz", () =>
            {
                _currentQuizIndex = 0;
                _quizScore = 0;
                SwitchPage(4); // Reload quiz page
            });
        }

        /// <summary>
        /// Versão atualizada do CloseModal que fecha modais de todas as páginas
        /// </summary>
        private void CloseModal()
        {
            // Fechar modal da página de categorias
            if (_categoriesPage != null)
                _categoriesPage.CloseModal();

            // Fechar outros modais se houver
            if (_openModal != null)
            {
                Destroy(_openModal);
                _openModal = null;
            }
        }
        private Transform CreateModalScrollView(Transform parent, Vector2 offsetMin, Vector2 offsetMax)
        {
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(parent, false);

            var sRT = scrollGO.GetComponent<RectTransform>();
            sRT.anchorMin = Vector2.zero;
            sRT.anchorMax = Vector2.one;
            sRT.offsetMin = offsetMin;
            sRT.offsetMax = offsetMax;

            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.05f);

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vRT = viewport.GetComponent<RectTransform>();
            vRT.anchorMin = Vector2.zero;
            vRT.anchorMax = Vector2.one;
            vRT.offsetMin = Vector2.zero;
            vRT.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            scrollRect.viewport = vRT;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.offsetMin = Vector2.zero;
            cRT.offsetMax = Vector2.zero;
            scrollRect.content = cRT;

            var vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return content.transform;
        }


        private void ShowPieceDetail(ComputerPiece piece)
        {
            CloseModal();

            var overlay = new GameObject("PieceDetailOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
            overlay.transform.SetParent(_canvas.transform, false);
            overlay.transform.SetAsLastSibling();
            _openModal = overlay;

            var ovRT = overlay.GetComponent<RectTransform>();
            ovRT.anchorMin = Vector2.zero;
            ovRT.anchorMax = Vector2.one;
            ovRT.offsetMin = Vector2.zero;
            ovRT.offsetMax = Vector2.zero;

            overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            overlay.GetComponent<Button>().onClick.AddListener(CloseModal);

            // Full screen detail view with gradient background
            var detailPanel = new GameObject("DetailPanel", typeof(RectTransform), typeof(Image));
            detailPanel.transform.SetParent(overlay.transform, false);

            var dRT = detailPanel.GetComponent<RectTransform>();
            dRT.anchorMin = Vector2.zero;
            dRT.anchorMax = Vector2.one;
            dRT.offsetMin = Vector2.zero;
            dRT.offsetMax = Vector2.zero;

            var dImg = detailPanel.GetComponent<Image>();
            dImg.sprite = _fallbackGradient;
            dImg.color = Color.white;

            // Header with back button
            var headerGO = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            headerGO.transform.SetParent(detailPanel.transform, false);
            var headerRT = headerGO.GetComponent<RectTransform>();
            headerRT.anchorMin = new Vector2(0, 1);
            headerRT.anchorMax = new Vector2(1, 1);
            headerRT.pivot = new Vector2(0.5f, 1);
            headerRT.offsetMin = new Vector2(16, -80);
            headerRT.offsetMax = new Vector2(-16, -16);

            var headerHLG = headerGO.GetComponent<HorizontalLayoutGroup>();
            headerHLG.childAlignment = TextAnchor.MiddleLeft;
            headerHLG.spacing = 12;

            // Back button
            var backBtn = new GameObject("BackBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            backBtn.transform.SetParent(headerGO.transform, false);
            var backRT = backBtn.GetComponent<RectTransform>();
            backRT.sizeDelta = new Vector2(40, 40);
            backBtn.GetComponent<Image>().color = new Color(1, 1, 1, 0.2f);
            backBtn.GetComponent<Button>().onClick.AddListener(CloseModal);

            UiKit.TMP(backBtn.transform, "←", 24, Color.white, TextAlignmentOptions.Center);

            // App title
            UiKit.TMP(headerGO.transform, "RetroTech", 28, Color.white, TextAlignmentOptions.Left, bold: true);

            // Content scroll view
            var contentScroll = CreateModalScrollView(detailPanel.transform, new Vector2(16, 16), new Vector2(-16, -96));

            // Piece details
            AddDetailSection(contentScroll, piece);
        }

        private void AddDetailSection(Transform parent, ComputerPiece piece)
        {
            // Hero image placeholder
            var heroCard = CreateGlassCard(parent, 200f);
            heroCard.color = new Color(1f, 1f, 1f, 0.15f);

            // Title
            UiKit.TMP(parent, piece.Name, 32, Color.white, TextAlignmentOptions.Left, bold: true);

            // Year
            AddDetailField(parent, "Ano de fabricação", piece.YearManufactured > 0 ? piece.YearManufactured.ToString() : "-");

            // Manufacturer
            AddDetailField(parent, "Fabricante", !string.IsNullOrEmpty(piece.Manufacturer) ? piece.Manufacturer : "-");

            // Description
            AddDetailSection(parent, "Descrição", !string.IsNullOrEmpty(piece.Description) ? piece.Description : "Sem descrição.");

            // Curiosities
            AddDetailSection(parent, "Curiosidades", !string.IsNullOrEmpty(piece.Curiosities) ? piece.Curiosities : "—");


            AddSpacer(parent, 40);
        }


        private void AddDetailField(Transform parent, string label, string value)
        {
            UiKit.TMP(parent, label, 18, new Color32(255, 255, 255, 150), TextAlignmentOptions.Left);
            var valueTMP = UiKit.TMP(parent, value, 20, Color.white, TextAlignmentOptions.Left, bold: true);
            valueTMP.margin = new Vector4(0, 0, 0, 16);
        }

        private void AddDetailSection(Transform parent, string title, string content)
        {
            UiKit.TMP(parent, title, 24, Color.white, TextAlignmentOptions.Left, bold: true);
            var contentTMP = UiKit.TMP(parent, content, 18, new Color32(255, 255, 255, 200), TextAlignmentOptions.Left);
            contentTMP.enableWordWrapping = true;
            contentTMP.margin = new Vector4(0, 0, 0, 20);
        }

        // ========= Scanner =========

        private void SimulateScan()
        {
#if UNITY_ANDROID
            try
            {
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                    UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            }
            catch { }
#endif

#if ZXING_PRESENT
            StartCoroutine(ScanQRCode());
#else
            if (SampleData.Pieces.Count == 0) return;
            int index = Random.Range(0, SampleData.Pieces.Count);
            ShowPieceDetail(SampleData.Pieces[index]);
#endif
        }

#if ZXING_PRESENT
        private System.Collections.IEnumerator ScanQRCode()
        {
            if (WebCamTexture.devices.Length == 0)
            {
                if (SampleData.Pieces.Count > 0)
                    ShowPieceDetail(SampleData.Pieces[Random.Range(0, SampleData.Pieces.Count)]);
                yield break;
            }

            var device = WebCamTexture.devices[0];
            var webcam = new WebCamTexture(device.name);
            webcam.Play();
            yield return new WaitForSeconds(0.5f);

            var reader = new BarcodeReader { AutoRotate = true, Options = new DecodingOptions { TryHarder = true } };
            ComputerPiece found = null;

            for (int attempt = 0; attempt < 30 && found == null; attempt++)
            {
                try
                {
                    var result = reader.Decode(webcam.GetPixels32(), webcam.width, webcam.height);
                    if (result != null)
                    {
                        string payload = result.Text;
                        const string prefix = "https://retro.tech/piece/";
                        if (payload.StartsWith(prefix)) payload = payload.Substring(prefix.Length);
                        foreach (var piece in SampleData.Pieces)
                            if (piece.Id == payload) { found = piece; break; }
                    }
                }
                catch { }
                yield return null;
            }
            webcam.Stop();

            if (found != null) ShowPieceDetail(found);
            else if (SampleData.Pieces.Count > 0) ShowPieceDetail(SampleData.Pieces[Random.Range(0, SampleData.Pieces.Count)]);
        }
#endif

        // ========= Search Feature =========

        private void CreateOrShowSearchPage()
        {
            if (_pages.Length < 6)
            {
                var newPages = new List<GameObject>(_pages);
                var searchPage = CreateSearchPage();
                newPages.Add(searchPage);
                _pages = newPages.ToArray();
                AddSearchNavTab();
            }
            SwitchPage(_pages.Length - 1);
        }

        private void AddSearchNavTab()
        {
            var row = _navBar.transform.Find("Background/IconsRow");
            CreatePrototypeNavTab(row, "Pesquisa", _pages.Length - 1, _iconHome);
            RefreshTabsVisual();
        }

        private GameObject CreateSearchPage()
        {
            var (surface, content) = BuildPrototypeSurface("SearchPage");

            // Search title
            UiKit.TMP(content, "Pesquisar Peças", 36, Color.white, TextAlignmentOptions.Left, bold: true);

            // Search input
            var searchCard = CreateGlassCard(content, 48f);
            searchCard.color = new Color(1f, 1f, 1f, 0.15f);

            var inputField = searchCard.gameObject.AddComponent<InputField>();

            // Placeholder
            var placeholderGO = new GameObject("Placeholder");
            placeholderGO.transform.SetParent(searchCard.transform, false);
            var placeholder = placeholderGO.AddComponent<Text>();
            placeholder.text = "Buscar por nome...";
            placeholder.color = new Color(1f, 1f, 1f, 0.5f);
            placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            placeholder.fontSize = 18;
            placeholder.alignment = TextAnchor.MiddleLeft;

            var phRT = placeholderGO.GetComponent<RectTransform>();
            phRT.anchorMin = Vector2.zero;
            phRT.anchorMax = Vector2.one;
            phRT.offsetMin = new Vector2(16, 0);
            phRT.offsetMax = new Vector2(-16, 0);

            // Text component
            var textGO = new GameObject("Text");
            textGO.transform.SetParent(searchCard.transform, false);
            var text = textGO.AddComponent<Text>();
            text.color = Color.white;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleLeft;

            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(16, 0);
            textRT.offsetMax = new Vector2(-16, 0);

            inputField.textComponent = text;
            inputField.placeholder = placeholder;

            // Sort dropdown
            var sortCard = CreateGlassCard(content, 48f);
            sortCard.color = new Color(1f, 1f, 1f, 0.15f);

            var dropdown = sortCard.gameObject.AddComponent<Dropdown>();
            dropdown.options = new List<Dropdown.OptionData> {
                new Dropdown.OptionData("Alfabético"),
                new Dropdown.OptionData("Ano crescente"),
                new Dropdown.OptionData("Ano decrescente")
            };

            // Search button
            var searchBtn = CreatePrototypeCTAButton(content.transform, "Buscar", () =>
            {
                PerformSearch(inputField.text, dropdown.value, content);
            });

            // Results area
            var resultsGO = new GameObject("SearchResults", typeof(RectTransform), typeof(LayoutElement));
            resultsGO.transform.SetParent(content, false);
            var resultsLE = resultsGO.GetComponent<LayoutElement>();
            resultsLE.flexibleHeight = 1;
            resultsLE.minHeight = 200;

            return surface;
        }

        private void PerformSearch(string query, int sortType, Transform contentParent)
        {
            // Find existing results area or create new one
            var resultsGO = contentParent.Find("SearchResults");
            if (resultsGO == null) return;

            // Clear previous results
            foreach (Transform child in resultsGO)
                Destroy(child.gameObject);

            // Filter pieces
            var filtered = new List<ComputerPiece>();
            foreach (var piece in SampleData.Pieces)
            {
                if (string.IsNullOrEmpty(query) || piece.Name.ToLower().Contains(query.ToLower()))
                    filtered.Add(piece);
            }

            // Sort pieces
            switch (sortType)
            {
                case 0: // Alphabetical
                    filtered.Sort((a, b) => a.Name.CompareTo(b.Name));
                    break;
                case 1: // Year ascending
                    filtered.Sort((a, b) => a.YearManufactured.CompareTo(b.YearManufactured));
                    break;
                case 2: // Year descending
                    filtered.Sort((a, b) => b.YearManufactured.CompareTo(a.YearManufactured));
                    break;
            }

            // Add vertical layout to results
            var vlg = resultsGO.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = resultsGO.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 8;
                vlg.childControlWidth = true;
                vlg.childForceExpandWidth = true;
                vlg.childControlHeight = true;
                vlg.childForceExpandHeight = false;
            }

            // Display results
            foreach (var piece in filtered)
            {
                var resultCard = CreateGlassCard(resultsGO, 56f);
                resultCard.color = new Color(1f, 1f, 1f, 0.1f);

                var btn = resultCard.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => ShowPieceDetail(piece));

                var resultTMP = UiKit.TMP(resultCard.transform,
                    $"{piece.Name} ({piece.YearManufactured}) - {piece.Description}",
                    16, Color.white, TextAlignmentOptions.MidlineLeft);
                resultTMP.enableWordWrapping = false;
                resultTMP.overflowMode = TMPro.TextOverflowModes.Ellipsis;
                resultTMP.raycastTarget = false;

                var resultRT = resultTMP.rectTransform;
                resultRT.anchorMin = Vector2.zero;
                resultRT.anchorMax = Vector2.one;
                resultRT.offsetMin = new Vector2(16, 8);
                resultRT.offsetMax = new Vector2(-16, -8);
            }

            // Add results count
            if (filtered.Count == 0)
            {
                var noResultsTMP = UiKit.TMP(resultsGO, "Nenhuma peça encontrada.", 18, new Color32(255, 255, 255, 150), TextAlignmentOptions.Center);
            }
        }

        // ========= Utility Methods =========

        private void AddSpacer(Transform parent, float height)
        {
            var spacerGO = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacerGO.transform.SetParent(parent, false);
            var le = spacerGO.GetComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
        }
    }
}