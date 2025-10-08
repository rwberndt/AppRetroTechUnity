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

        [Header("Content Configuration")]
        [SerializeField] private float titleFontSize = 60f;
        [SerializeField] private float welcomeFontSize = 42f;
        [SerializeField] private float descriptionFontSize = 32f;
        [SerializeField] private float sectionTitleFontSize = 34f;
        [SerializeField] private float sectionTextFontSize = 28f;

        // Colors
        private readonly Color TextMain = new Color32(245, 245, 255, 255);
        private readonly Color TextMuted = new Color32(210, 210, 235, 255);
        private readonly Color PrimaryColor = new Color32(114, 74, 160, 255);
        private readonly Color PanelGradientTop = new Color32(189, 164, 255, 255);
        private readonly Color PanelGradientBottom = new Color32(255, 124, 208, 255);
        private readonly Color PanelShadowColor = new Color(0f, 0f, 0f, 0.25f);

        // Events
        public System.Action OnSearchPiecesClicked;

        private GameObject _pageObject;
        private RectTransform _contentContainer;
        private RectTransform _contentPanel;
        private Sprite _panelBackgroundSprite;

        /// <summary>
        /// Cria e configura a página inicial
        /// </summary>
        /// <param name="parent">Transform pai onde a página será criada</param>
        /// <param name="buildSurfaceFunc">Função para criar a superfície base da página</param>
        /// <returns>GameObject da página criada</returns>
        public GameObject CreatePage(Transform parent, System.Func<string, (GameObject surface, RectTransform content)> buildSurfaceFunc)
        {
            var (surface, content) = buildSurfaceFunc(pageTitle);
            _pageObject = surface.gameObject;
            _contentContainer = content;

            ConfigureSurfaceLayout();
            CreateContent();

            return _pageObject;
        }

        /// <summary>
        /// Cria todo o conteúdo da página inicial
        /// </summary>
        private void CreateContent()
        {
            _contentPanel = CreateContentPanel();

            CreateTitle();
            CreateWelcomeSection();
            CreateDescriptionSection();
            CreateObjectiveSection();
            CreateFeaturesSection();
            CreateTeamSection();
            CreateCTAButton();
        }

        /// <summary>
        /// Ajusta o layout padrão da superfície base para preencher a tela.
        /// </summary>
        private void ConfigureSurfaceLayout()
        {
            if (_contentContainer == null)
            {
                return;
            }

            var layout = _contentContainer.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(32, 32, 48, 48);
                layout.spacing = 32f;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childForceExpandWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandHeight = false;
            }
        }

        /// <summary>
        /// Cria o painel principal que contém todo o conteúdo textual.
        /// </summary>
        private RectTransform CreateContentPanel()
        {
            var panelGO = new GameObject("HomePanel", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelGO.transform.SetParent(_contentContainer, false);

            var rect = panelGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = panelGO.GetComponent<Image>();
            image.sprite = EnsureRoundedGradient(ref _panelBackgroundSprite, PanelGradientTop, PanelGradientBottom, 46f, 512, 1536);
            image.type = Image.Type.Simple;
            image.color = Color.white;

            var shadow = panelGO.AddComponent<Shadow>();
            shadow.effectColor = PanelShadowColor;
            shadow.effectDistance = new Vector2(0f, 12f);
            shadow.useGraphicAlpha = true;

            var layout = panelGO.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 64, 64);
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            var fitter = panelGO.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var layoutElement = panelGO.AddComponent<LayoutElement>();
            layoutElement.minHeight = 0f;
            layoutElement.flexibleHeight = 0f;

            return rect;
        }

        /// <summary>
        /// Cria o título principal do aplicativo
        /// </summary>
        private void CreateTitle()
        {
            var titleTMP = UiKit.TMP(_contentPanel, "RetroTech", (int)titleFontSize, TextMain,
                TextAlignmentOptions.Left, bold: true);
            titleTMP.name = "Title";
            titleTMP.enableWordWrapping = false;
            ApplyBottomSpacing(titleTMP, 12f);
        }

        /// <summary>
        /// Cria a seção de boas-vindas
        /// </summary>
        private void CreateWelcomeSection()
        {
            var welcomeTMP = UiKit.TMP(_contentPanel, "Bem-vindo ao RetroTech", (int)welcomeFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            welcomeTMP.name = "WelcomeTitle";
            welcomeTMP.enableWordWrapping = false;
            ApplyBottomSpacing(welcomeTMP);
        }

        /// <summary>
        /// Cria a descrição principal do aplicativo
        /// </summary>
        private void CreateDescriptionSection()
        {
            var descriptionText = "Explore e aprenda sobre o acervo de peças de computação do " +
                                "Departamento de Sistemas e Computação (DSC) da FURB de forma interativa.";

            var descTMP = UiKit.TMP(_contentPanel, descriptionText, (int)descriptionFontSize,
                TextMuted, TextAlignmentOptions.Left);
            descTMP.name = "MainDescription";
            descTMP.enableWordWrapping = true;
            ApplyBottomSpacing(descTMP, 18f);
        }

        /// <summary>
        /// Cria a seção sobre o objetivo do aplicativo
        /// </summary>
        private void CreateObjectiveSection()
        {
            // Título da seção
            var objTitleTMP = UiKit.TMP(_contentPanel, "Objetivo do aplicativo", (int)sectionTitleFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            objTitleTMP.name = "ObjectiveTitle";
            ApplyBottomSpacing(objTitleTMP, 6f);

            // Descrição do objetivo
            var objectiveText = "Facilitar o acesso e a compreensão do acervo histórico de peças de " +
                              "computação do DSC, proporcionando uma experiência educativa e imersiva.";

            var objDescTMP = UiKit.TMP(_contentPanel, objectiveText, (int)sectionTextFontSize,
                TextMuted, TextAlignmentOptions.Left);
            objDescTMP.name = "ObjectiveDescription";
            objDescTMP.enableWordWrapping = true;
            ApplyBottomSpacing(objDescTMP, 18f);
        }

        /// <summary>
        /// Cria a seção das principais funcionalidades
        /// </summary>
        private void CreateFeaturesSection()
        {
            // Título da seção
            var featTitleTMP = UiKit.TMP(_contentPanel, "Principais funcionalidades", (int)sectionTitleFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            featTitleTMP.name = "FeaturesTitle";
            ApplyBottomSpacing(featTitleTMP, 6f);

            // Lista de funcionalidades
            var featuresText = string.Join("\n", new[]
            {
                "• Navegação por categorias de peças",
                "• Linha do tempo interativa",
                "• Leitura de QR Codes",
                "• Detalhes e curiosidades",
                "• Quiz educativo"
            });

            var featDescTMP = UiKit.TMP(_contentPanel, featuresText, (int)sectionTextFontSize,
                TextMuted, TextAlignmentOptions.Left);
            featDescTMP.name = "FeaturesDescription";
            featDescTMP.enableWordWrapping = true;
            ApplyBottomSpacing(featDescTMP, 18f);
        }

        /// <summary>
        /// Cria a seção da equipe de desenvolvimento
        /// </summary>
        private void CreateTeamSection()
        {
            // Título da seção
            var teamTitleTMP = UiKit.TMP(_contentPanel, "Equipe de desenvolvimento", (int)sectionTitleFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            teamTitleTMP.name = "TeamTitle";
            ApplyBottomSpacing(teamTitleTMP, 6f);

            // Informações da equipe
            var teamText = "Integrante: Aruto Gherono de Souza dos Santos - Sistemas de Informação\n" +
                          "Orientador: Dalton Solano dos Reis\n" +
                          "Supervisor: Miguel A. Wistainater";

            var teamDescTMP = UiKit.TMP(_contentPanel, teamText, (int)sectionTextFontSize,
                TextMuted, TextAlignmentOptions.Left);
            teamDescTMP.name = "TeamDescription";
            teamDescTMP.enableWordWrapping = true;
            ApplyBottomSpacing(teamDescTMP, 26f);
        }

        /// <summary>
        /// Cria o botão de call-to-action para buscar peças
        /// </summary>
        private void CreateCTAButton()
        {
            var buttonCard = UiKit.CreateCard(_contentPanel.transform, new Vector2(0, 72),
                Color.white, 26f);

            var buttonLE = buttonCard.GetComponent<LayoutElement>();
            if (buttonLE == null)
            {
                buttonLE = buttonCard.gameObject.AddComponent<LayoutElement>();
            }
            buttonLE.minHeight = 72;
            buttonLE.preferredHeight = 72;
            buttonLE.flexibleWidth = 1f;

            var button = buttonCard.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => OnSearchPiecesClicked?.Invoke());

            var buttonText = UiKit.TMP(buttonCard.transform, "Buscar Peças", 30,
                PrimaryColor, TextAlignmentOptions.Center, bold: true);
            buttonText.name = "CTAButtonText";
            buttonText.enableWordWrapping = false;

            // Posicionar o texto no centro do botão
            var textRT = buttonText.rectTransform;
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(16, 8);
            textRT.offsetMax = new Vector2(-16, -8);

            var buttonRT = buttonCard.rectTransform;
            buttonRT.anchorMin = new Vector2(0f, 1f);
            buttonRT.anchorMax = new Vector2(1f, 1f);
            buttonRT.pivot = new Vector2(0.5f, 1f);
            buttonRT.offsetMin = Vector2.zero;
            buttonRT.offsetMax = Vector2.zero;

            var buttonShadow = buttonCard.gameObject.AddComponent<Shadow>();
            buttonShadow.effectColor = new Color(0f, 0f, 0f, 0.18f);
            buttonShadow.effectDistance = new Vector2(0f, 6f);
            buttonShadow.useGraphicAlpha = true;

            buttonCard.name = "CTAButton";
        }

        private void ApplyBottomSpacing(TextMeshProUGUI tmp, float margin = 12f)
        {
            if (tmp == null)
            {
                return;
            }

            tmp.margin = new Vector4(tmp.margin.x, tmp.margin.y, tmp.margin.z, margin);
        }

        /// <summary>
        /// Atualiza as configurações visuais da página
        /// </summary>
        /// <param name="newTitleSize">Novo tamanho da fonte do título</param>
        /// <param name="newWelcomeSize">Novo tamanho da fonte de boas-vindas</param>
        /// <param name="newDescSize">Novo tamanho da fonte de descrição</param>
        public void UpdateFontSizes(float newTitleSize, float newWelcomeSize, float newDescSize)
        {
            titleFontSize = newTitleSize;
            welcomeFontSize = newWelcomeSize;
            descriptionFontSize = newDescSize;

            if (_contentPanel != null)
            {
                var title = _contentPanel.Find("Title")?.GetComponent<TextMeshProUGUI>();
                if (title != null) title.fontSize = titleFontSize;

                var welcome = _contentPanel.Find("WelcomeTitle")?.GetComponent<TextMeshProUGUI>();
                if (welcome != null) welcome.fontSize = welcomeFontSize;

                var desc = _contentPanel.Find("MainDescription")?.GetComponent<TextMeshProUGUI>();
                if (desc != null) desc.fontSize = descriptionFontSize;
            }
        }

        /// <summary>
        /// Obtém referência ao GameObject da página
        /// </summary>
        public GameObject GetPageObject()
        {
            return _pageObject;
        }

        /// <summary>
        /// Obtém referência ao container de conteúdo
        /// </summary>
        public RectTransform GetContentContainer()
        {
            return _contentContainer;
        }

        /// <summary>
        /// Define se a página está ativa ou não
        /// </summary>
        public void SetActive(bool active)
        {
            if (_pageObject != null)
            {
                _pageObject.SetActive(active);
            }
        }

        /// <summary>
        /// Limpa recursos da página
        /// </summary>
        private void OnDestroy()
        {
            OnSearchPiecesClicked = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Valida as configurações no editor
        /// </summary>
        private void OnValidate()
        {
            titleFontSize = Mathf.Max(10f, titleFontSize);
            welcomeFontSize = Mathf.Max(10f, welcomeFontSize);
            descriptionFontSize = Mathf.Max(10f, descriptionFontSize);
            sectionTitleFontSize = Mathf.Max(10f, sectionTitleFontSize);
            sectionTextFontSize = Mathf.Max(10f, sectionTextFontSize);
        }
#endif

        /// <summary>
        /// Cria (ou reutiliza) um sprite de gradiente vertical com cantos arredondados.
        /// </summary>
        private Sprite EnsureRoundedGradient(ref Sprite cache, Color top, Color bottom, float radius, int width = 512, int height = 512)
        {
            if (cache != null)
            {
                return cache;
            }

            cache = CreateRoundedGradientSprite(top, bottom, radius, width, height);
            return cache;
        }

        /// <summary>
        /// Gera dinamicamente uma textura com gradiente vertical e bordas arredondadas.
        /// </summary>
        private Sprite CreateRoundedGradientSprite(Color top, Color bottom, float radius, int width, int height)
        {
            width = Mathf.Max(8, width);
            height = Mathf.Max(8, height);
            radius = Mathf.Clamp(radius, 0f, Mathf.Min(width, height) * 0.5f);

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Color[] pixels = new Color[width * height];
            float radiusSq = radius * radius;
            float rightLimit = width - radius;
            float topLimit = height - radius;

            for (int y = 0; y < height; y++)
            {
                float t = y / (height - 1f);
                Color rowColor = Color.Lerp(bottom, top, t);

                for (int x = 0; x < width; x++)
                {
                    bool inside = true;
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    if (x < radius && y < radius)
                    {
                        float dx = radius - px;
                        float dy = radius - py;
                        inside = dx * dx + dy * dy <= radiusSq;
                    }
                    else if (x >= rightLimit && y < radius)
                    {
                        float dx = px - rightLimit;
                        float dy = radius - py;
                        inside = dx * dx + dy * dy <= radiusSq;
                    }
                    else if (x < radius && y >= topLimit)
                    {
                        float dx = radius - px;
                        float dy = py - topLimit;
                        inside = dx * dx + dy * dy <= radiusSq;
                    }
                    else if (x >= rightLimit && y >= topLimit)
                    {
                        float dx = px - rightLimit;
                        float dy = py - topLimit;
                        inside = dx * dx + dy * dy <= radiusSq;
                    }

                    pixels[y * width + x] = inside ? rowColor : new Color(0f, 0f, 0f, 0f);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = $"RoundedGradient_{width}x{height}";
            return sprite;
        }
    }
}
