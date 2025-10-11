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

        [Header("Layout")]
        [SerializeField] private float heroMinimumHeight = 280f;
        [SerializeField] private float sectionSpacing = 20f;

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
        private GameObject _pageObject;
        private Sprite _heroGradientSprite;
        private TextMeshProUGUI _appTitleTMP;
        private TextMeshProUGUI _welcomeTMP;
        private readonly List<TextMeshProUGUI> _bodyTexts = new();

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

            if (_appTitleTMP != null)
                _appTitleTMP.fontSize = appTitleFontSize;

            if (_welcomeTMP != null)
                _welcomeTMP.fontSize = welcomeFontSize;

            foreach (var text in _bodyTexts)
            {
                if (text != null)
                    text.fontSize = bodyFontSize;
            }
        }

        private void ConfigureContentLayout()
        {
            if (_contentContainer == null)
                return;

            var layout = _contentContainer.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(36, 36, 48, 48);
                layout.spacing = sectionSpacing;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandHeight = false;
            }
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
            var layoutElement = heroCard.GetComponent<LayoutElement>();
            layoutElement.minHeight = heroMinimumHeight;

            _appTitleTMP = TMP(heroCard.transform, "RetroTech", appTitleFontSize, TextMain,
                TextAlignmentOptions.Left, bold: true);
            _appTitleTMP.enableWordWrapping = false;
            _appTitleTMP.raycastTarget = false;

            _welcomeTMP = TMP(heroCard.transform, "Bem-vindo ao RetroTech", welcomeFontSize, TextMain,
                TextAlignmentOptions.Left, bold: true);
            _welcomeTMP.margin = new Vector4(0, 8, 0, 0);
            _welcomeTMP.raycastTarget = false;

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

            var label = TMP(buttonGO.transform, "Buscar Peças", 24, new Color32(103, 80, 164, 255),
                TextAlignmentOptions.Center, bold: true);
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            var labelRT = label.rectTransform;
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = new Vector2(20, 12);
            labelRT.offsetMax = new Vector2(-20, -12);

            var layout = buttonGO.AddComponent<LayoutElement>();
            layout.preferredHeight = 72f;
            layout.minHeight = 64f;
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
            vlg.padding = new RectOffset(32, 32, 36, 36);
            vlg.spacing = 12f;
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;

            var fitter = card.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layout = card.AddComponent<LayoutElement>();
            layout.minHeight = 0f;

            return card;
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
        }
    }
}
