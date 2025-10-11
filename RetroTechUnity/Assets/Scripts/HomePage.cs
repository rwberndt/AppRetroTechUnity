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
        [SerializeField] private float descriptionFontSize = 35f;
        [SerializeField] private float sectionTitleFontSize = 40f;
        [SerializeField] private float sectionTextFontSize = 35f;
        [SerializeField] private float cardMinimumHeight = 180f;

        // Colors
        private readonly Color TextMain = new Color32(245, 245, 255, 255);
        private readonly Color TextMuted = new Color32(210, 210, 235, 255);
        private readonly Color PrimaryColor = new Color32(114, 74, 160, 255);
        private readonly Color CardColor = new Color32(255, 255, 255, 30);

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
            CreateWelcomeSection();
            CreateDescriptionSection();
            CreateObjectiveSection();
            CreateFeaturesSection();
            CreateHighlightsSection();
            CreateHowToExploreSection();
            CreateTeamSection();
            CreateVisitSection();
            CreateFeedbackSection();
            CreateCTAButton();
        }

        /// <summary>
        /// Cria o título principal do aplicativo
        /// </summary>
        private void CreateTitle()
        {
            var titleTMP = UiKit.TMP(_contentContainer, "RetroTech", (int)titleFontSize, TextMain,
                TextAlignmentOptions.Left, bold: true);
            titleTMP.name = "Title";
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
            var objTitleTMP = UiKit.TMP(_contentContainer, "Objetivo do aplicativo", (int)sectionTitleFontSize,
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
            var featTitleTMP = UiKit.TMP(_contentContainer, "Principais funcionalidades", (int)sectionTitleFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            featTitleTMP.name = "FeaturesTitle";

            // Lista de funcionalidades
            var featuresText = " Navegação por categorias\n" +
                             " Linha do tempo interativa\n" +
                             " Leitura de QR Codes\n" +
                             " Detalhes e curiosidades\n" +
                             " Quiz educativo";

            var featDescTMP = UiKit.TMP(_contentContainer, featuresText, (int)sectionTextFontSize,
                TextMuted, TextAlignmentOptions.Left);
            featDescTMP.name = "FeaturesDescription";
        }

        /// <summary>
        /// Cria a seção da equipe de desenvolvimento
        /// </summary>
        private void CreateTeamSection()
        {
            // Título da seção
            var teamTitleTMP = UiKit.TMP(_contentContainer, "Equipe de desenvolvimento", (int)sectionTitleFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            teamTitleTMP.name = "TeamTitle";

            // Informações da equipe
            var teamText = "Ricardo Berndt - Ciência da Computação\n\n" +
                          "Orientador: Dalton Solano dos Reis\n" +
                          "Supervisor: Miguel A. Wistainater";

            var teamDescTMP = UiKit.TMP(_contentContainer, teamText, (int)sectionTextFontSize,
                TextMuted, TextAlignmentOptions.Left);
            teamDescTMP.name = "TeamDescription";
        }

        /// <summary>
        /// Cria um card responsivo reaproveitado pelas seções expandidas.
        /// </summary>
        private Image CreateResponsiveCard(string name)
        {
            var card = UiKit.CreateCard(_contentContainer.transform, new Vector2(0, cardMinimumHeight),
                CardColor, 22f, glass: true);

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(28, 28, 24, 24);
            layout.spacing = 16f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var le = card.GetComponent<LayoutElement>();
            le.minHeight = cardMinimumHeight;
            le.preferredHeight = -1f;

            card.gameObject.name = name;

            return card;
        }

        /// <summary>
        /// Cria uma seção com destaques adicionais do acervo.
        /// </summary>
        private void CreateHighlightsSection()
        {
            var card = CreateResponsiveCard("HighlightsCard");

            var title = UiKit.TMP(card.transform, "Destaques do acervo", (int)sectionTitleFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            title.enableWordWrapping = true;

            var bulletText =
                "• Mais de 40 anos de evolução em um acervo curado pela comunidade acadêmica.\n" +
                "• Peças restauradas com descrições técnicas, curiosidades e imagens de alta resolução.\n" +
                "• Conteúdo alinhado com disciplinas de História da Computação, Arquitetura e Engenharias.";

            var description = UiKit.TMP(card.transform, bulletText, (int)sectionTextFontSize,
                TextMuted, TextAlignmentOptions.Left);
            description.enableWordWrapping = true;
            description.margin = new Vector4(0, 0, 0, 8f);

            var complement = UiKit.TMP(card.transform,
                "Todo o material segue um layout responsivo, ideal para consultar rapidamente durante visitas guiadas ou aulas.",
                Mathf.Max(20, (int)sectionTextFontSize - 4), TextMuted, TextAlignmentOptions.Left);
            complement.enableWordWrapping = true;
        }

        /// <summary>
        /// Cria uma seção com orientações de uso do aplicativo.
        /// </summary>
        private void CreateHowToExploreSection()
        {
            var card = CreateResponsiveCard("HowToExploreCard");

            var title = UiKit.TMP(card.transform, "Como explorar o RetroTech", (int)sectionTitleFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            title.enableWordWrapping = true;

            var steps =
                "1. Use a busca por categorias para encontrar rapidamente um tema específico.\n" +
                "2. Acesse a linha do tempo para comparar as peças por ano de fabricação.\n" +
                "3. Utilize o scanner QR durante a visita física para abrir os detalhes instantaneamente.\n" +
                "4. Finalize com o quiz para testar o que aprendeu e compartilhar o resultado com colegas.";

            var stepsText = UiKit.TMP(card.transform, steps, (int)sectionTextFontSize,
                TextMuted, TextAlignmentOptions.Left);
            stepsText.enableWordWrapping = true;

            var tip = UiKit.TMP(card.transform,
                "O layout da página se adapta de forma fluida a diferentes larguras de tela, mantendo margens confortáveis mesmo em smartphones compactos.",
                Mathf.Max(20, (int)sectionTextFontSize - 4), TextMuted, TextAlignmentOptions.Left);
            tip.enableWordWrapping = true;
        }

        /// <summary>
        /// Cria uma seção com informações para visitas e acessibilidade.
        /// </summary>
        private void CreateVisitSection()
        {
            var card = CreateResponsiveCard("VisitInfoCard");

            var title = UiKit.TMP(card.transform, "Planeje sua visita", (int)sectionTitleFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            title.enableWordWrapping = true;

            var visitText =
                "• Horários sugeridos: segunda a sexta-feira, das 8h às 12h e das 13h às 18h.\n" +
                "• As peças mais procuradas estão sinalizadas com QR Codes próximos ao pedestal.\n" +
                "• Recursos de contraste e tamanhos de fonte ajustáveis diretamente pelo sistema do seu dispositivo.";

            var visitTMP = UiKit.TMP(card.transform, visitText, (int)sectionTextFontSize,
                TextMuted, TextAlignmentOptions.Left);
            visitTMP.enableWordWrapping = true;

            var complementary = UiKit.TMP(card.transform,
                "Para visitas em grupo, entre em contato com o DSC para agendar uma experiência mediada pela equipe do RetroTech.",
                Mathf.Max(20, (int)sectionTextFontSize - 4), TextMuted, TextAlignmentOptions.Left);
            complementary.enableWordWrapping = true;
        }

        /// <summary>
        /// Cria uma seção reforçando feedbacks e contribuições.
        /// </summary>
        private void CreateFeedbackSection()
        {
            var card = CreateResponsiveCard("FeedbackCard");

            var title = UiKit.TMP(card.transform, "Compartilhe descobertas", (int)sectionTitleFontSize,
                TextMain, TextAlignmentOptions.Left, bold: true);
            title.enableWordWrapping = true;

            var text =
                "Gostou de alguma peça ou percebeu algo que pode ser aprimorado? Utilize o formulário disponível no portal do DSC ou fale diretamente com a equipe para sugerir novos registros, fotos ou depoimentos de ex-alunos.";

            var feedbackTMP = UiKit.TMP(card.transform, text, (int)sectionTextFontSize,
                TextMuted, TextAlignmentOptions.Left);
            feedbackTMP.enableWordWrapping = true;

            var closing = UiKit.TMP(card.transform,
                "O retorno constante da comunidade garante que o aplicativo continue atualizado e acessível em smartphones de diferentes resoluções.",
                Mathf.Max(20, (int)sectionTextFontSize - 4), TextMuted, TextAlignmentOptions.Left);
            closing.enableWordWrapping = true;
        }

        /// <summary>
        /// Cria o botão de call-to-action para buscar peças
        /// </summary>
        private void CreateCTAButton()
        {
            var buttonCard = UiKit.CreateCard(_contentContainer.transform, new Vector2(0, 60),
                Color.white, 22f);

            var buttonLE = buttonCard.gameObject.AddComponent<LayoutElement>();
            buttonLE.minHeight = 60;
            buttonLE.preferredHeight = 60;

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

            // Atualizar elementos existentes se a página já foi criada
            if (_contentContainer != null)
            {
                var title = _contentContainer.Find("Title")?.GetComponent<TextMeshProUGUI>();
                if (title != null) title.fontSize = titleFontSize;

                var welcome = _contentContainer.Find("WelcomeTitle")?.GetComponent<TextMeshProUGUI>();
                if (welcome != null) welcome.fontSize = welcomeFontSize;

                var desc = _contentContainer.Find("MainDescription")?.GetComponent<TextMeshProUGUI>();
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
            cardMinimumHeight = Mathf.Max(80f, cardMinimumHeight);
        }
#endif
        #endregion
    }
}