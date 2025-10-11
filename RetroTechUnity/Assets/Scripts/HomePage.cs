using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static RetroTech.UiKit;

namespace RetroTech
{
    /// <summary>
    /// Página inicial do RetroTech que apresenta informações sobre o aplicativo,
    /// seus objetivos, funcionalidades principais e equipe de desenvolvimento.
    /// </summary>
    public class HomePage : MonoBehaviour
    {
        [Header("Page Configuration")]
        [SerializeField] private string pageTitle = "HomePage";

        [Header("Typography")]
        [SerializeField] private int appTitleFontSize = 52;
        [SerializeField] private int welcomeFontSize = 30;
        [SerializeField] private int sectionTitleFontSize = 24;
        [SerializeField] private int bodyFontSize = 20;
        [SerializeField] private int callToActionFontSize = 24;

        [Header("Layout")]
        [SerializeField] private float heroMinimumHeight = 280f;
        [SerializeField] private float sectionSpacing = 20f;
        [SerializeField] private Vector4 contentPadding = new Vector4(36f, 36f, 48f, 48f);
        [SerializeField] private Vector4 cardPadding = new Vector4(32f, 32f, 36f, 36f);
        [SerializeField] private float cardSpacing = 12f;
        [SerializeField] private Vector2 callToActionPadding = new Vector2(20f, 12f);
        [SerializeField] private Vector2 callToActionHeight = new Vector2(64f, 72f);

        [Header("Responsive")]
        [SerializeField] private bool adaptToScreenSize = true;
        [SerializeField] private Vector2 referenceResolution = new Vector2(1080f, 1920f);
        [SerializeField, Range(0.5f, 2f)] private float minimumScale = 0.85f;
        [SerializeField, Range(0.5f, 2f)] private float maximumScale = 1.3f;

        [Header("Colors")]
        [SerializeField] private Color heroGradientTop = new Color32(189, 164, 255, 255);
        [SerializeField] private Color heroGradientBottom = new Color32(255, 124, 208, 255);
        [SerializeField] private Color cardBackground = new Color(1f, 1f, 1f, 0.14f);
        [SerializeField] private Color sectionTitleColor = TextMain;
        [SerializeField] private Color sectionBodyColor = TextMuted;

        /// <summary>
        /// Disparado quando o usuário pressiona o botão "Buscar Peças".
        /// </summary>
        public event Action OnSearchPiecesClicked;

        private RectTransform _contentContainer;
        private VerticalLayoutGroup _contentLayoutGroup;
        private GameObject _pageObject;
        private Sprite _heroGradientSprite;
        private TextMeshProUGUI _appTitleTMP;
        private TextMeshProUGUI _welcomeTMP;
        private TextMeshProUGUI _callToActionTMP;
        private RectTransform _callToActionLabelRect;
        private LayoutElement _callToActionLayoutElement;
        private readonly List<TextMeshProUGUI> _bodyTexts = new();
        private readonly List<TextMeshProUGUI> _sectionTitleTexts = new();
        private readonly Dictionary<TextMeshProUGUI, Vector4> _textBaseMargins = new();

        private readonly List<CardLayoutInfo> _cardLayouts = new();
        private Vector2 _lastScreenSize;
        private float _currentScale = 1f;
        private bool _pageBuilt;
        private bool _isApplyingResponsiveScale;

        private int _appTitleFontSizeBase;
        private int _welcomeFontSizeBase;
        private int _sectionTitleFontSizeBase;
        private int _bodyFontSizeBase;
        private int _callToActionFontSizeBase;
        private float _heroMinimumHeightBase;
        private float _sectionSpacingBase;
        private float _cardSpacingBase;
        private Vector4 _contentPaddingBase;
        private Vector4 _cardPaddingBase;
        private Vector2 _callToActionPaddingBase;
        private Vector2 _callToActionHeightBase;
        private float _sectionTitleToBodyRatio;
        private float _callToActionToBodyRatio;

        private struct CardLayoutInfo
        {
            public VerticalLayoutGroup LayoutGroup;
            public LayoutElement LayoutElement;
            public bool IsHero;
        }

        private void Awake()
        {
            CacheBaseValues();
        }

        /// <summary>
        /// Cria a página inicial utilizando a superfície base fornecida pelo GameManager.
        /// </summary>
        /// <param name="parent">Transform pai da página.</param>
        /// <param name="buildSurfaceFunc">Função utilizada para criar a superfície scrollável padrão.</param>
        /// <returns>GameObject raiz da página criada.</returns>
        public GameObject CreatePage(Transform parent, Func<string, (GameObject surface, RectTransform content)> buildSurfaceFunc)
        {
            var (surface, content) = buildSurfaceFunc(pageTitle);
            _pageObject = surface;
            _contentContainer = content;

            ConfigureContentLayout();
            BuildPageContent();

            _pageBuilt = true;
            ApplyResponsiveSizing(true);

            return _pageObject;
        }

        /// <summary>
        /// Atualiza dinamicamente os tamanhos de fonte principais da página.
        /// </summary>
        /// <param name="titleSize">Novo tamanho da fonte do título "RetroTech".</param>
        /// <param name="welcomeSize">Novo tamanho da fonte do subtítulo de boas-vindas.</param>
        /// <param name="descSize">Novo tamanho base dos textos descritivos.</param>
        public void UpdateFontSizes(float titleSize, float welcomeSize, float descSize)
        {
            appTitleFontSize = Mathf.RoundToInt(titleSize);
            welcomeFontSize = Mathf.RoundToInt(welcomeSize);
            bodyFontSize = Mathf.RoundToInt(descSize);

            if (!_isApplyingResponsiveScale)
            {
                _appTitleFontSizeBase = appTitleFontSize;
                _welcomeFontSizeBase = welcomeFontSize;
                _bodyFontSizeBase = bodyFontSize;
            }

            if (_appTitleTMP != null)
                _appTitleTMP.fontSize = appTitleFontSize;

            if (_welcomeTMP != null)
                _welcomeTMP.fontSize = welcomeFontSize;

            foreach (var text in _bodyTexts)
            {
                if (text != null)
                    text.fontSize = bodyFontSize;
            }

            if (!_isApplyingResponsiveScale)
            {
                var sectionSize = Mathf.RoundToInt(bodyFontSize * _sectionTitleToBodyRatio);
                UpdateSectionTitleFontSizes(sectionSize);

                callToActionFontSize = Mathf.RoundToInt(bodyFontSize * _callToActionToBodyRatio);
                _callToActionFontSizeBase = callToActionFontSize;
                _callToActionToBodyRatio = bodyFontSize > 0 ? callToActionFontSize / (float)bodyFontSize : _callToActionToBodyRatio;
                if (_callToActionTMP != null)
                    _callToActionTMP.fontSize = callToActionFontSize;
            }
        }

        private void ConfigureContentLayout()
        {
            if (_contentContainer == null)
                return;

            var layout = _contentContainer.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                _contentLayoutGroup = layout;
                ApplyPadding(layout.padding, contentPadding);
                layout.spacing = sectionSpacing;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
            }
        }

        private void CacheBaseValues()
        {
            _appTitleFontSizeBase = appTitleFontSize;
            _welcomeFontSizeBase = welcomeFontSize;
            _sectionTitleFontSizeBase = sectionTitleFontSize;
            _bodyFontSizeBase = bodyFontSize;
            _callToActionFontSizeBase = callToActionFontSize;
            _heroMinimumHeightBase = heroMinimumHeight;
            _sectionSpacingBase = sectionSpacing;
            _cardSpacingBase = cardSpacing;
            _contentPaddingBase = contentPadding;
            _cardPaddingBase = cardPadding;
            _callToActionPaddingBase = callToActionPadding;
            _callToActionHeightBase = callToActionHeight;
            _sectionTitleToBodyRatio = bodyFontSize > 0 ? sectionTitleFontSize / (float)bodyFontSize : 1f;
            _callToActionToBodyRatio = bodyFontSize > 0 ? callToActionFontSize / (float)bodyFontSize : 1f;
        }

        private void BuildPageContent()
        {
            CreateHeroSection();
            CreateObjectiveSection();
            CreateFeaturesSection();
            CreateTeamSection();
            CreateCallToActionButton();
        }

        private void CreateHeroSection()
        {
            var heroCard = CreateFlexibleCard("HeroSection", heroGradient: true);

            _appTitleTMP = TMP(heroCard.transform, "RetroTech", appTitleFontSize, TextMain,
                TextAlignmentOptions.Left, bold: true);
            _appTitleTMP.enableWordWrapping = false;
            _appTitleTMP.raycastTarget = false;
            StoreBaseMargin(_appTitleTMP);

            _welcomeTMP = TMP(heroCard.transform, "Bem-vindo ao RetroTech", welcomeFontSize, TextMain,
                TextAlignmentOptions.Left, bold: true);
            _welcomeTMP.margin = new Vector4(0, 8, 0, 0);
            _welcomeTMP.raycastTarget = false;
            StoreBaseMargin(_welcomeTMP);

            var heroDescription = TMP(heroCard.transform,
                "Explore e aprenda sobre o acervo de peças de computação do Departamento de Sistemas e Computação (DSC) da FURB de forma interativa.",
                bodyFontSize, sectionBodyColor, TextAlignmentOptions.Left);
            heroDescription.margin = new Vector4(0, 12, 0, heroDescription.margin.w);
            RegisterBodyText(heroDescription);
        }

        private void CreateObjectiveSection()
        {
            var objectiveCard = CreateFlexibleCard("ObjectiveSection");

            var objectiveTitle = TMP(objectiveCard.transform, "Objetivo do aplicativo", sectionTitleFontSize, sectionTitleColor,
                TextAlignmentOptions.Left, bold: true);
            objectiveTitle.raycastTarget = false;
            RegisterSectionTitle(objectiveTitle);

            RegisterBodyText(TMP(objectiveCard.transform,
                "Facilitar o acesso e a compreensão do acervo histórico de computação do DSC, proporcionando uma experiência educativa e imersiva.",
                bodyFontSize, sectionBodyColor, TextAlignmentOptions.Left));
        }

        private void CreateFeaturesSection()
        {
            var featuresCard = CreateFlexibleCard("FeaturesSection");

            var featuresTitle = TMP(featuresCard.transform, "Principais funcionalidades", sectionTitleFontSize, sectionTitleColor,
                TextAlignmentOptions.Left, bold: true);
            featuresTitle.raycastTarget = false;
            RegisterSectionTitle(featuresTitle);

            foreach (var feature in GetFeatureHighlights())
            {
                RegisterBodyText(TMP(featuresCard.transform, $"• {feature}", bodyFontSize, sectionBodyColor,
                    TextAlignmentOptions.Left));
            }
        }

        private void CreateTeamSection()
        {
            var teamCard = CreateFlexibleCard("TeamSection");

            var teamTitle = TMP(teamCard.transform, "Equipe de desenvolvimento", sectionTitleFontSize, sectionTitleColor,
                TextAlignmentOptions.Left, bold: true);
            teamTitle.raycastTarget = false;
            RegisterSectionTitle(teamTitle);

            RegisterBodyText(TMP(teamCard.transform, "Integrantes: Darlan Junior de Souza dos Santos - Sistemas de Informação",
                bodyFontSize, sectionBodyColor, TextAlignmentOptions.Left));

            RegisterBodyText(TMP(teamCard.transform, "Designer: Dalton Solano dos Reis",
                bodyFontSize, sectionBodyColor, TextAlignmentOptions.Left));

            RegisterBodyText(TMP(teamCard.transform, "Supervisor: Miguel A. Wisintainer",
                bodyFontSize, sectionBodyColor, TextAlignmentOptions.Left));
        }

        private void CreateCallToActionButton()
        {
            var buttonGO = new GameObject("SearchPiecesButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(_contentContainer, false);

            var rt = buttonGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var image = buttonGO.GetComponent<Image>();
            image.color = Color.white;
            image.raycastTarget = true;

            var button = buttonGO.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            button.onClick.AddListener(() => OnSearchPiecesClicked?.Invoke());

            _callToActionTMP = TMP(buttonGO.transform, "Buscar Peças", callToActionFontSize, new Color32(103, 80, 164, 255),
                TextAlignmentOptions.Center, bold: true);
            _callToActionTMP.enableWordWrapping = false;
            _callToActionTMP.raycastTarget = false;
            StoreBaseMargin(_callToActionTMP);

            _callToActionLabelRect = _callToActionTMP.rectTransform;
            _callToActionLabelRect.anchorMin = Vector2.zero;
            _callToActionLabelRect.anchorMax = Vector2.one;
            _callToActionLabelRect.offsetMin = new Vector2(callToActionPadding.x, callToActionPadding.y);
            _callToActionLabelRect.offsetMax = new Vector2(-callToActionPadding.x, -callToActionPadding.y);

            _callToActionLayoutElement = buttonGO.AddComponent<LayoutElement>();
            _callToActionLayoutElement.preferredHeight = callToActionHeight.y;
            _callToActionLayoutElement.minHeight = callToActionHeight.x;
        }

        private GameObject CreateFlexibleCard(string name, bool heroGradient = false)
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            card.transform.SetParent(_contentContainer, false);

            var rt = card.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = card.GetComponent<Image>();
            img.color = heroGradient ? Color.white : cardBackground;
            img.raycastTarget = false;

            if (heroGradient)
            {
                img.sprite = EnsureHeroGradientSprite();
                img.type = Image.Type.Simple;
            }

            var vlg = card.GetComponent<VerticalLayoutGroup>();
            ApplyPadding(vlg.padding, cardPadding);
            vlg.spacing = cardSpacing;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;

            var fitter = card.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layout = card.AddComponent<LayoutElement>();
            layout.minHeight = heroGradient ? heroMinimumHeight : 0f;

            _cardLayouts.Add(new CardLayoutInfo
            {
                LayoutGroup = vlg,
                LayoutElement = layout,
                IsHero = heroGradient
            });

            return card;
        }

        private void RegisterSectionTitle(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            _sectionTitleTexts.Add(text);
            StoreBaseMargin(text);
        }

        private Sprite EnsureHeroGradientSprite()
        {
            if (_heroGradientSprite != null)
                return _heroGradientSprite;

            const int textureHeight = 96;
            var texture = new Texture2D(1, textureHeight)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            var pixels = new Color[textureHeight];
            for (int i = 0; i < textureHeight; i++)
            {
                float t = i / (textureHeight - 1f);
                pixels[i] = Color.Lerp(heroGradientBottom, heroGradientTop, t);
            }

            texture.SetPixels(pixels);
            texture.Apply();

            _heroGradientSprite = Sprite.Create(texture, new Rect(0, 0, 1, textureHeight), new Vector2(0.5f, 0.5f));
            return _heroGradientSprite;
        }

        private IEnumerable<string> GetFeatureHighlights()
        {
            yield return "Navegação por categorias de peças, timeline histórica e quiz interativo";
            yield return "Leitura de QR codes para acessar informações diretamente no acervo";
            yield return "Cards detalhados com fotos, curiosidades e dados das peças";
            yield return "Interface responsiva inspirada no protótipo oficial do projeto";
        }

        private void RegisterBodyText(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            text.fontSize = bodyFontSize;
            text.enableWordWrapping = true;
            text.raycastTarget = false;

            var margin = text.margin;
            margin.w = Mathf.Max(margin.w, 8f); // bottom

            if (!string.IsNullOrEmpty(text.text) && text.text.TrimStart().StartsWith("•"))
                margin.x = Mathf.Max(margin.x, 12f); // indent bullet list

            text.margin = margin;
            _bodyTexts.Add(text);
            StoreBaseMargin(text);
        }

        private void StoreBaseMargin(TextMeshProUGUI text)
        {
            if (text == null)
                return;

            _textBaseMargins[text] = text.margin;
        }

        private void ApplyResponsiveSizing(bool forceUpdate = false)
        {
            if (!adaptToScreenSize || !_pageBuilt)
                return;

            var currentSize = new Vector2(Screen.width, Screen.height);
            if (!forceUpdate && currentSize == _lastScreenSize)
                return;

            var reference = referenceResolution;
            if (reference.x <= 0f || reference.y <= 0f)
                reference = new Vector2(1080f, 1920f);

            float widthRatio = currentSize.x / reference.x;
            float heightRatio = currentSize.y / reference.y;
            float scale = Mathf.Clamp(Mathf.Min(widthRatio, heightRatio), minimumScale, maximumScale);

            if (!forceUpdate && Mathf.Approximately(scale, _currentScale))
            {
                _lastScreenSize = currentSize;
                return;
            }

            _lastScreenSize = currentSize;
            _currentScale = scale;

            _isApplyingResponsiveScale = true;

            UpdateFontSizes(_appTitleFontSizeBase * scale, _welcomeFontSizeBase * scale, _bodyFontSizeBase * scale);
            UpdateSectionTitleFontSizes(Mathf.RoundToInt(_sectionTitleFontSizeBase * scale));

            callToActionFontSize = Mathf.RoundToInt(_callToActionFontSizeBase * scale);
            if (_callToActionTMP != null)
                _callToActionTMP.fontSize = callToActionFontSize;

            if (_contentLayoutGroup != null)
            {
                ApplyPadding(_contentLayoutGroup.padding, _contentPaddingBase * scale);
                _contentLayoutGroup.spacing = _sectionSpacingBase * scale;
            }

            foreach (var card in _cardLayouts)
            {
                if (card.LayoutGroup != null)
                {
                    ApplyPadding(card.LayoutGroup.padding, _cardPaddingBase * scale);
                    card.LayoutGroup.spacing = _cardSpacingBase * scale;
                }

                if (card.LayoutElement != null)
                    card.LayoutElement.minHeight = card.IsHero ? _heroMinimumHeightBase * scale : 0f;
            }

            if (_callToActionLayoutElement != null)
            {
                var scaledHeight = new Vector2(_callToActionHeightBase.x * scale, _callToActionHeightBase.y * scale);
                _callToActionLayoutElement.preferredHeight = scaledHeight.y;
                _callToActionLayoutElement.minHeight = scaledHeight.x;
                callToActionHeight = scaledHeight;
            }

            if (_callToActionLabelRect != null)
            {
                var padding = new Vector2(_callToActionPaddingBase.x * scale, _callToActionPaddingBase.y * scale);
                callToActionPadding = padding;
                _callToActionLabelRect.offsetMin = padding;
                _callToActionLabelRect.offsetMax = new Vector2(-padding.x, -padding.y);
            }

            heroMinimumHeight = _heroMinimumHeightBase * scale;
            sectionSpacing = _sectionSpacingBase * scale;
            cardSpacing = _cardSpacingBase * scale;
            contentPadding = new Vector4(_contentPaddingBase.x * scale, _contentPaddingBase.y * scale,
                _contentPaddingBase.z * scale, _contentPaddingBase.w * scale);
            cardPadding = new Vector4(_cardPaddingBase.x * scale, _cardPaddingBase.y * scale,
                _cardPaddingBase.z * scale, _cardPaddingBase.w * scale);

            foreach (var kvp in _textBaseMargins)
            {
                var text = kvp.Key;
                if (text == null)
                    continue;

                var baseMargin = kvp.Value;
                text.margin = baseMargin * scale;
            }

            _isApplyingResponsiveScale = false;
        }

        private static void ApplyPadding(RectOffset target, Vector4 padding)
        {
            if (target == null)
                return;

            target.left = Mathf.RoundToInt(padding.x);
            target.right = Mathf.RoundToInt(padding.y);
            target.top = Mathf.RoundToInt(padding.z);
            target.bottom = Mathf.RoundToInt(padding.w);
        }

        private void UpdateSectionTitleFontSizes(int newSize)
        {
            sectionTitleFontSize = newSize;

            foreach (var text in _sectionTitleTexts)
            {
                if (text != null)
                    text.fontSize = newSize;
            }

            if (!_isApplyingResponsiveScale)
            {
                _sectionTitleFontSizeBase = newSize;
                _sectionTitleToBodyRatio = bodyFontSize > 0 ? newSize / (float)bodyFontSize : _sectionTitleToBodyRatio;
            }
        }

        private void Update()
        {
            ApplyResponsiveSizing();
        }
    }
}
