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
        [SerializeField] private float welcomeFontSize = 40f;
        [SerializeField] private float descriptionFontSize = 34f;
        [SerializeField] private float sectionTitleFontSize = 38f;
        [SerializeField] private float sectionTextFontSize = 32f;

        [Header("Palette")]
        [SerializeField] private Color gradientTop = new Color32(138, 98, 221, 255);
        [SerializeField] private Color gradientBottom = new Color32(255, 146, 196, 255);
        [SerializeField] private Color buttonFill = new Color32(245, 245, 255, 255);
        [SerializeField] private float heroCornerRadius = 44f;
        [SerializeField] private float buttonCornerRadius = 28f;

        private readonly Color TextMain = new Color32(245, 245, 255, 255);
        private readonly Color TextMuted = new Color32(215, 214, 238, 255);
        private readonly Color PrimaryColor = new Color32(114, 74, 160, 255);

        // Events
        public System.Action OnSearchPiecesClicked;

        private GameObject _pageObject;
        private RectTransform _contentContainer;
        private RectTransform _heroCard;

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

            CreateContent();

            return _pageObject;
        }

        /// <summary>
        /// Cria todo o conteúdo da página inicial
        /// </summary>
        private void CreateContent()
        {
            _heroCard = CreateHeroCard();

            CreateTitle();
            CreateWelcomeSection();
            CreateDescriptionSection();
            CreateObjectiveSection();
            CreateFeaturesSection();
            CreateTeamSection();
            CreateSpacer(32f, "SpacerBeforeCTA");
            CreateCTAButton();
        }

        private RectTransform CreateHeroCard()
        {
            var cardGO = new GameObject("HeroCard", typeof(RectTransform));
            cardGO.transform.SetParent(_contentContainer, false);

            var rt = cardGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            var layoutElement = cardGO.AddComponent<LayoutElement>();
            layoutElement.minHeight = 0f;
            layoutElement.flexibleWidth = 1f;
            layoutElement.preferredWidth = 760f;

            var background = cardGO.AddComponent<Image>();
            background.raycastTarget = false;
            ApplyVerticalGradient(background, gradientTop, gradientBottom);
            TryApplyRoundedCorners(cardGO, heroCornerRadius);

            var layoutGroup = cardGO.AddComponent<VerticalLayoutGroup>();
            layoutGroup.padding = new RectOffset(56, 56, 72, 72);
            layoutGroup.spacing = 32f;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandHeight = false;

            cardGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return rt;
        }

        /// <summary>
        /// Cria o título principal do aplicativo
        /// </summary>
        private void CreateTitle()
        {
            var titleTMP = UiKit.TMP(_heroCard, "RetroTech", (int)titleFontSize, (Color32)TextMain,
                TextAlignmentOptions.Left, bold: true);
            titleTMP.name = "Title";
            titleTMP.enableWordWrapping = false;
        }

        /// <summary>
        /// Cria a seção de boas-vindas
        /// </summary>
        private void CreateWelcomeSection()
        {
            var welcomeTMP = UiKit.TMP(_heroCard, "Bem-vindo ao RetroTech", (int)welcomeFontSize,
                (Color32)TextMain, TextAlignmentOptions.Left, bold: true);
            welcomeTMP.name = "WelcomeTitle";
            welcomeTMP.enableWordWrapping = false;
        }

        /// <summary>
        /// Cria a descrição principal do aplicativo
        /// </summary>
        private void CreateDescriptionSection()
        {
            var descriptionText = "Explore e aprenda sobre o acervo de peças de computação do " +
                                "Departamento de Sistemas e Computação (DSC) da FURB com uma experiência interativa e envolvente.";

            var descTMP = UiKit.TMP(_heroCard, descriptionText, (int)descriptionFontSize,
                (Color32)TextMuted, TextAlignmentOptions.Left);
            descTMP.name = "MainDescription";
        }

        /// <summary>
        /// Cria a seção sobre o objetivo do aplicativo
        /// </summary>
        private void CreateObjectiveSection()
        {
            var objectiveText = "Facilitar o acesso e a compreensão do acervo histórico de peças de " +
                              "computação do DSC, proporcionando uma experiência educativa e imersiva.";

            CreateInfoSection("Objective", "Objetivo do aplicativo", objectiveText);
        }

        /// <summary>
        /// Cria a seção das principais funcionalidades
        /// </summary>
        private void CreateFeaturesSection()
        {
            var featuresText = "• Navegação por categorias de peças\n" +
                             "• Linha do tempo interativa\n" +
                             "• Leitor de QR Codes\n" +
                             "• Quiz educativo";

            CreateInfoSection("Features", "Principais funcionalidades", featuresText);
        }

        /// <summary>
        /// Cria a seção da equipe de desenvolvimento
        /// </summary>
        private void CreateTeamSection()
        {
            var teamText = "Integrante: Darlon Solano dos Reis - Sistemas de Informação\n" +
                          "Orientador: Dalton Solano dos Reis\n" +
                          "Supervisor: Miguel A. Wistainater";

            CreateInfoSection("Team", "Equipe de desenvolvimento", teamText);
        }

        private void CreateInfoSection(string sectionKey, string title, string body)
        {
            var section = new GameObject($"{sectionKey}Section", typeof(RectTransform));
            section.transform.SetParent(_heroCard, false);

            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var titleTMP = UiKit.TMP(section.transform, title, (int)sectionTitleFontSize,
                (Color32)TextMain, TextAlignmentOptions.Left, bold: true);
            titleTMP.name = $"{sectionKey}Title";

            var bodyTMP = UiKit.TMP(section.transform, body, (int)sectionTextFontSize,
                (Color32)TextMuted, TextAlignmentOptions.Left);
            bodyTMP.name = $"{sectionKey}Description";
        }

        private void CreateSpacer(float height, string name)
        {
            var spacer = new GameObject(name, typeof(RectTransform));
            spacer.transform.SetParent(_heroCard, false);
            var layout = spacer.AddComponent<LayoutElement>();
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }

        /// <summary>
        /// Cria o botão de call-to-action para buscar peças
        /// </summary>
        private void CreateCTAButton()
        {
            var buttonCard = UiKit.CreateCard(_heroCard.transform, new Vector2(0, 96),
                (Color32)buttonFill, buttonCornerRadius);

            var buttonLE = buttonCard.gameObject.GetComponent<LayoutElement>();
            buttonLE.minHeight = 96f;
            buttonLE.preferredHeight = 96f;
            buttonLE.flexibleHeight = 0f;

            var button = buttonCard.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.92f);
            colors.pressedColor = new Color(0.92f, 0.92f, 0.98f, 1f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0.95f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnSearchPiecesClicked?.Invoke());

            var buttonText = UiKit.TMP(buttonCard.transform, "Buscar Peças", 32,
                (Color32)PrimaryColor, TextAlignmentOptions.Center, bold: true);
            buttonText.name = "CTAButtonText";
            buttonText.enableWordWrapping = false;

            var textRT = buttonText.rectTransform;
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(28f, 18f);
            textRT.offsetMax = new Vector2(-28f, -18f);

            buttonCard.gameObject.name = "CTAButton";
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

            // Atualizar elementos existentes se a página já foi criada
            if (_contentContainer != null)
            {
                var title = _contentContainer.Find("HeroCard/Title")?.GetComponent<TextMeshProUGUI>();
                if (title != null) title.fontSize = titleFontSize;

                var welcome = _contentContainer.Find("HeroCard/WelcomeTitle")?.GetComponent<TextMeshProUGUI>();
                if (welcome != null) welcome.fontSize = welcomeFontSize;

                var desc = _contentContainer.Find("HeroCard/MainDescription")?.GetComponent<TextMeshProUGUI>();
                if (desc != null) desc.fontSize = descriptionFontSize;
            }
        }

        /// <summary>
        /// Obtém referência ao GameObject da página
        /// </summary>
        /// <returns>GameObject da página ou null se não foi criada</returns>
        public GameObject GetPageObject()
        {
            return _pageObject;
        }

        /// <summary>
        /// Obtém referência ao container de conteúdo
        /// </summary>
        /// <returns>RectTransform do container de conteúdo ou null se não foi criado</returns>
        public RectTransform GetContentContainer()
        {
            return _contentContainer;
        }

        /// <summary>
        /// Define se a página está ativa ou não
        /// </summary>
        /// <param name="active">Estado de ativação</param>
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

        #region Editor Methods
#if UNITY_EDITOR
        /// <summary>
        /// Valida as configurações no editor
        /// </summary>
        private void OnValidate()
        {
            // Garantir que os tamanhos de fonte sejam válidos
            titleFontSize = Mathf.Max(10f, titleFontSize);
            welcomeFontSize = Mathf.Max(10f, welcomeFontSize);
            descriptionFontSize = Mathf.Max(10f, descriptionFontSize);
            sectionTitleFontSize = Mathf.Max(10f, sectionTitleFontSize);
            sectionTextFontSize = Mathf.Max(10f, sectionTextFontSize);
            heroCornerRadius = Mathf.Max(0f, heroCornerRadius);
            buttonCornerRadius = Mathf.Max(0f, buttonCornerRadius);
        }
#endif
        #endregion
    }
}
