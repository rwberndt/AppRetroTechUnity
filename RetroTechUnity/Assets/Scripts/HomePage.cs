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
        [SerializeField] private float titleFontSize = DefaultPageTitleFontSize;
        [SerializeField] private float welcomeFontSize = 52f;
        [SerializeField] private float descriptionFontSize = 44f;
        [SerializeField] private float sectionTitleFontSize = 50f;
        [SerializeField] private float sectionTextFontSize = 42f;
        [SerializeField] private float sectionSpacing = 50f;

        // Colors
        private readonly Color TextMain = new Color32(245, 245, 255, 255);
        private readonly Color TextMuted = new Color32(210, 210, 235, 255);
        private readonly Color PrimaryColor = new Color32(114, 74, 160, 255);

        // Events
        public System.Action OnSearchPiecesClicked;

        private GameObject _pageObject;
        private RectTransform _contentContainer;

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
            CreateTitle();
            AddSpacing(30f);
            CreateWelcomeSection();
            AddSpacing(20f);
            CreateDescriptionSection();
            AddSpacing(sectionSpacing);
            CreateObjectiveSection();
            AddSpacing(sectionSpacing);
            CreateFeaturesSection();
            AddSpacing(sectionSpacing);
            CreateTeamSection();
            AddSpacing(sectionSpacing);
            CreateCTAButton();
        }

        /// <summary>
        /// Adiciona um espaçador vertical ao conteúdo.
        /// </summary>
        /// <param name="customSpacing">Espaçamento customizado (opcional)</param>
        private void AddSpacing(float? customSpacing = null)
        {
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(_contentContainer, false);

            var spacerLE = spacer.AddComponent<LayoutElement>();
            float spacing = ResponsiveTypography.ResponsiveSpacing(customSpacing ?? sectionSpacing);

            spacerLE.minHeight = spacing;
            spacerLE.preferredHeight = spacing;
        }

        /// <summary>
        /// Cria o título principal do aplicativo
        /// </summary>
        private void CreateTitle()
        {
            var (titleContainer, titleTMP) = UiKit.CreatePageTitle(_contentContainer, "RetroTech",
                Mathf.RoundToInt(titleFontSize), TextMain, TextAlignmentOptions.Left);
            titleContainer.name = "TitleContainer";

            if (titleTMP != null)
            {
                titleTMP.name = "Title";
                titleTMP.raycastTarget = false;
            }
        }

        /// <summary>
        /// Cria a seção de boas-vindas
        /// </summary>
        private void CreateWelcomeSection()
        {
            var welcomeTMP = UiKit.TMP(_contentContainer, "Bem-vindo ao RetroTech", (int)welcomeFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            welcomeTMP.name = "WelcomeTitle";
        }

        /// <summary>
        /// Cria a descrição principal do aplicativo
        /// </summary>
        private void CreateDescriptionSection()
        {
            var descriptionText = "Explore e aprenda sobre o acervo de peças de computação do " +
                                "Departamento de Sistemas e Computação (DSC) da FURB de forma interativa.";

            var descTMP = UiKit.TMP(_contentContainer, descriptionText, (int)descriptionFontSize,
                TextMuted, TextAlignmentOptions.Left);
            descTMP.name = "MainDescription";
        }

        /// <summary>
        /// Cria a seção sobre o objetivo do aplicativo
        /// </summary>
        private void CreateObjectiveSection()
        {
            // Título da seção
            var objTitleTMP = UiKit.TMP(_contentContainer, "Objetivo do aplicativo\n", (int)sectionTitleFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            objTitleTMP.name = "ObjectiveTitle";

            // Descrição do objetivo
            var objectiveText = "Facilitar o acesso e a compreensão do acervo histórico de peças de " +
                              "computação do DSC, proporcionando uma experiência educativa e imersiva.";

            var objDescTMP = UiKit.TMP(_contentContainer, objectiveText, (int)sectionTextFontSize,
                TextMuted, TextAlignmentOptions.Left);
            objDescTMP.name = "ObjectiveDescription";
        }

        /// <summary>
        /// Cria a seção das principais funcionalidades
        /// </summary>
        private void CreateFeaturesSection()
        {
            // Título da seção
            var featTitleTMP = UiKit.TMP(_contentContainer, "Principais funcionalidades\n", (int)sectionTitleFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            featTitleTMP.name = "FeaturesTitle";

            // Lista de funcionalidades
            var featuresText = "• Navegação por categorias\n" +
                             "• Linha do tempo interativa\n" +
                             "• Leitura de QR Codes\n" +
                             "• Detalhes e curiosidades\n" +
                             "• Quiz educativo";

            var featDescTMP = UiKit.TMP(_contentContainer, featuresText, (int)sectionTextFontSize,
                TextMuted, TextAlignmentOptions.Left);
            featDescTMP.name = "FeaturesDescription";
        }

        /// <summary>
        /// Cria a seção com informações da equipe do projeto.
        /// </summary>
        private void CreateTeamSection()
        {
            var teamTitleTMP = UiKit.TMP(_contentContainer, "Equipe de desenvolvimento\n", (int)sectionTitleFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            teamTitleTMP.name = "TeamTitle";

            var teamText = "Ricardo Berndt - Ciência da Computação\n" +
                          "Orientador: Dalton Solano dos Reis\n" +
                          "Supervisor: Miguel A. Wistainater";

            var teamDescTMP = UiKit.TMP(_contentContainer, teamText, (int)sectionTextFontSize,
                TextMuted, TextAlignmentOptions.Left);
            teamDescTMP.name = "TeamDescription";
        }

        /// <summary>
        /// Cria o botão de call-to-action para buscar peças
        /// </summary>
        private void CreateCTAButton()
        {
            const float baseButtonHeight = 80f;
            var buttonCard = UiKit.CreateCard(_contentContainer.transform, new Vector2(0, baseButtonHeight),
                Color.white, 22f);

            var buttonLE = buttonCard.gameObject.GetComponent<LayoutElement>();
            if (buttonLE != null)
            {
                float responsiveHeight = ResponsiveTypography.ResponsiveSpacing(baseButtonHeight);
                buttonLE.minHeight = responsiveHeight;
                buttonLE.preferredHeight = responsiveHeight;
            }

            var button = buttonCard.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => OnSearchPiecesClicked?.Invoke());

            var buttonText = UiKit.TMP(buttonCard.transform, "Buscar Peças", 38,
                PrimaryColor, TextAlignmentOptions.Center, bold: true);
            buttonText.name = "CTAButtonText";
            buttonText.enableWordWrapping = false;

            // Posicionar o texto no centro do botão
            var textRT = buttonText.rectTransform;
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(16, 8);
            textRT.offsetMax = new Vector2(-16, -8);

            buttonCard.name = "CTAButton";
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

            ApplyResponsiveFontUpdates();
        }

        /// <summary>
        /// Aplica os tamanhos de fonte responsivos nos elementos principais já criados.
        /// </summary>
        private void ApplyResponsiveFontUpdates()
        {
            if (_contentContainer == null)
                return;

            ApplyResponsiveFontToChild("Title", titleFontSize);
            ApplyResponsiveFontToChild("WelcomeTitle", welcomeFontSize);
            ApplyResponsiveFontToChild("MainDescription", descriptionFontSize);
        }

        private void ApplyResponsiveFontToChild(string childName, float baseFontSize)
        {
            TextMeshProUGUI target = null;
            foreach (var tmp in _contentContainer.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp != null && tmp.name == childName)
                {
                    target = tmp;
                    break;
                }
            }

            if (target != null)
            {
                ResponsiveTypography.ApplyToTMP(target, Mathf.RoundToInt(baseFontSize));
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
            sectionSpacing = Mathf.Max(0f, sectionSpacing);
        }
#endif
        #endregion
    }
}


