using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static RetroTech.UiKit;

namespace RetroTech
{
    /// <summary>
    /// Página de categorias do RetroTech que permite navegação hierárquica
    /// através das diferentes categorias e subcategorias de peças computacionais.
    /// Inclui funcionalidade de expandir/recolher categorias e modal de peças.
    /// </summary>
    public class CategoriesPage : MonoBehaviour
    {
        [Header("Page Configuration")]
        [SerializeField] private string pageTitle = "CategoriesPage";

        [Header("Visual Configuration")]
        [SerializeField] private float categoryHeaderHeight = 56f;
        [SerializeField] private float subcategoryItemHeight = 48f;
        [SerializeField] private int categoryTitleFontSize = 18;
        [SerializeField] private int subcategoryFontSize = 18;
        [SerializeField] private float infoCardMinimumHeight = 160f;

        // Colors
        private readonly Color HeaderColor = new Color32(255, 255, 255, 38);
        private readonly Color SubcategoryColor = new Color32(255, 255, 255, 20);
        private readonly Color TextColor = Color.white;
        private readonly Color ChevronColor = new Color32(120, 100, 170, 255);
        private readonly Color InfoCardColor = new Color32(255, 255, 255, 26);
        private readonly Color InfoTextColor = new Color32(220, 220, 245, 255);
        private readonly Color ModalOverlayColor = new Color(0, 0, 0, 0.7f);
        private readonly Color ModalPanelColor = new Color(1f, 1f, 1f, 0.95f);
        private readonly Color ModalTextColor = new Color32(50, 50, 70, 255);

        // Events
        public System.Action<ComputerPiece> OnPieceSelected;

        // State
        private readonly Dictionary<long, bool> _categoryExpanded = new Dictionary<long, bool>();
        private GameObject _pageObject;
        private RectTransform _contentContainer;
        private Canvas _parentCanvas;
        private GameObject _openModal;

        /// <summary>
        /// Cria e configura a página de categorias
        /// </summary>
        /// <param name="parent">Transform pai onde a página será criada</param>
        /// <param name="canvas">Canvas pai para modais</param>
        /// <param name="buildSurfaceFunc">Função para criar a superfície base da página</param>
        /// <returns>GameObject da página criada</returns>
        public GameObject CreatePage(Transform parent, Canvas canvas, System.Func<string, (GameObject surface, RectTransform content)> buildSurfaceFunc)
        {
            _parentCanvas = canvas;
            var (surface, content) = buildSurfaceFunc(pageTitle);
            _pageObject = surface.gameObject;
            _contentContainer = content;

            CreateCategoriesContent();

            return _pageObject;
        }

        /// <summary>
        /// Cria todo o conteúdo das categorias
        /// </summary>
        private void CreateCategoriesContent()
        {
            CreateIntroSection();
            CreateLegendSection();
            CreateSummarySection();

            foreach (Category category in SampleData.Categories)
            {
                CreateCategorySection(category);
            }
        }

        /// <summary>
        /// Cria um card informativo com layout responsivo.
        /// </summary>
        private Image CreateInfoCard(string name)
        {
            var card = UiKit.CreateCard(_contentContainer.transform, new Vector2(0, infoCardMinimumHeight),
                InfoCardColor, 18f, glass: true);

            var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var le = card.GetComponent<LayoutElement>();
            le.minHeight = infoCardMinimumHeight;
            le.preferredHeight = -1f;

            card.gameObject.name = name;

            return card;
        }

        /// <summary>
        /// Cria a seção introdutória com orientações gerais.
        /// </summary>
        private void CreateIntroSection()
        {
            var card = CreateInfoCard("IntroCard");

            var title = UiKit.TMP(card.transform, "Organize sua exploração", categoryTitleFontSize + 6,
                TextColor, TextAlignmentOptions.Left, bold: true);
            title.enableWordWrapping = true;

            var introText =
                "A lista de categorias foi pensada para telas de diferentes tamanhos. Os elementos ajustam automaticamente margens e espaçamentos, garantindo leitura confortável em celulares compactos ou tablets maiores.";

            var introTMP = UiKit.TMP(card.transform, introText, subcategoryFontSize,
                InfoTextColor, TextAlignmentOptions.Left);
            introTMP.enableWordWrapping = true;
        }

        /// <summary>
        /// Cria uma seção com dicas de uso e acessibilidade.
        /// </summary>
        private void CreateLegendSection()
        {
            var card = CreateInfoCard("LegendCard");

            var title = UiKit.TMP(card.transform, "Dicas rápidas", categoryTitleFontSize + 4,
                TextColor, TextAlignmentOptions.Left, bold: true);
            title.enableWordWrapping = true;

            var legend =
                "• Toque no cabeçalho para expandir ou recolher cada categoria.\n" +
                "• Os subtítulos foram otimizados para até duas linhas; em telas menores o texto quebra automaticamente.\n" +
                "• Use gestos de rolagem com o polegar para percorrer longas listas sem perder o contexto.";

            var legendTMP = UiKit.TMP(card.transform, legend, subcategoryFontSize,
                InfoTextColor, TextAlignmentOptions.Left);
            legendTMP.enableWordWrapping = true;

            var extraTip = UiKit.TMP(card.transform,
                "Com fones ou leitores de tela, os títulos são lidos em sequência graças à hierarquia simples de botões e cartões.",
                Mathf.Max(12, subcategoryFontSize - 2), InfoTextColor, TextAlignmentOptions.Left);
            extraTip.enableWordWrapping = true;
        }

        /// <summary>
        /// Cria uma seção com estatísticas rápidas das categorias.
        /// </summary>
        private void CreateSummarySection()
        {
            var card = CreateInfoCard("SummaryCard");

            var title = UiKit.TMP(card.transform, "Resumo do acervo", categoryTitleFontSize + 4,
                TextColor, TextAlignmentOptions.Left, bold: true);
            title.enableWordWrapping = true;

            int categoryCount = SampleData.Categories.Count;
            int subcategoryCount = SampleData.Categories.Sum(c => c.Subcategories?.Count ?? 0);
            var largestCategory = SampleData.Categories
                .OrderByDescending(c => c.Subcategories?.Count ?? 0)
                .FirstOrDefault();

            var summaryText = UiKit.TMP(card.transform,
                $"{categoryCount} categorias principais • {subcategoryCount} subcategorias mapeadas",
                subcategoryFontSize, InfoTextColor, TextAlignmentOptions.Left);
            summaryText.enableWordWrapping = true;

            if (largestCategory != null)
            {
                var highlight = UiKit.TMP(card.transform,
                    $"Destaque: " +
                    $"{largestCategory.Name} reúne {largestCategory.Subcategories.Count} temas complementares.",
                    Mathf.Max(12, subcategoryFontSize - 2), InfoTextColor, TextAlignmentOptions.Left);
                highlight.enableWordWrapping = true;
            }

            var closing = UiKit.TMP(card.transform,
                "Atualizamos os dados conforme novas peças são catalogadas. Ao sincronizar, a página reorganiza os blocos sem perder fluidez na rolagem.",
                Mathf.Max(12, subcategoryFontSize - 2), InfoTextColor, TextAlignmentOptions.Left);
            closing.enableWordWrapping = true;
        }

        /// <summary>
        /// Cria uma seção completa de categoria com header e subcategorias
        /// </summary>
        /// <param name="category">Categoria a ser criada</param>
        private void CreateCategorySection(Category category)
        {
            // Criar header da categoria
            var (header, chevron) = CreateCategoryHeader(category.Name);

            // Criar lista de subcategorias
            var subList = CreateSubcategoryList(category);

            // Configurar estado inicial
            if (!_categoryExpanded.ContainsKey(category.Id))
                _categoryExpanded[category.Id] = false;

            subList.SetActive(_categoryExpanded[category.Id]);

            if (chevron != null)
                chevron.localEulerAngles = _categoryExpanded[category.Id] ? new Vector3(0, 0, 180) : Vector3.zero;

            // Configurar interação do header
            SetupCategoryHeaderInteraction(header, category.Id, subList, chevron);
        }

        /// <summary>
        /// Cria o header de uma categoria
        /// </summary>
        /// <param name="categoryName">Nome da categoria</param>
        /// <returns>Tupla com o GameObject do header e Transform do chevron</returns>
        private (GameObject header, RectTransform chevron) CreateCategoryHeader(string categoryName)
        {
            var headerCard = UiKit.CreateCard(_contentContainer.transform, new Vector2(0, categoryHeaderHeight),
                HeaderColor, 16f, glass: true);
            var header = headerCard.gameObject;

            // Adicionar botão ao header
            var btn = header.GetComponent<Button>() ?? header.AddComponent<Button>();
            btn.targetGraphic = headerCard;

            // Configurar cores do botão
            var colors = btn.colors;
            colors.highlightedColor = new Color(1, 1, 1, 0.1f);
            colors.pressedColor = new Color(1, 1, 1, 0.2f);
            btn.colors = colors;

            // Adicionar título
            var titleTMP = UiKit.TMP(header.transform, categoryName, categoryTitleFontSize,
                TextColor, TextAlignmentOptions.MidlineLeft, bold: true);
            titleTMP.enableWordWrapping = false;
            titleTMP.overflowMode = TMPro.TextOverflowModes.Overflow;
            titleTMP.rectTransform.anchorMin = new Vector2(0, 0);
            titleTMP.rectTransform.anchorMax = new Vector2(1, 1);
            titleTMP.rectTransform.offsetMin = new Vector2(16, 8);
            titleTMP.rectTransform.offsetMax = new Vector2(-44, -8);
            titleTMP.raycastTarget = false;

            // Criar chevron (seta)
            var chevron = CreateChevronIcon(header.transform);

            header.name = $"CategoryHeader_{categoryName}";

            return (header, chevron);
        }

        /// <summary>
        /// Cria o ícone chevron (seta) para o header da categoria
        /// </summary>
        /// <param name="parent">Transform pai</param>
        /// <returns>RectTransform do chevron</returns>
        private RectTransform CreateChevronIcon(Transform parent)
        {
            var chevronGO = new GameObject("Chevron", typeof(RectTransform), typeof(Image));
            chevronGO.transform.SetParent(parent, false);

            var chevronRT = chevronGO.GetComponent<RectTransform>();
            chevronRT.anchorMin = new Vector2(1, 0.5f);
            chevronRT.anchorMax = new Vector2(1, 0.5f);
            chevronRT.pivot = new Vector2(1, 0.5f);
            chevronRT.sizeDelta = new Vector2(18, 18);
            chevronRT.anchoredPosition = new Vector2(-14, 0);

            var chevronImg = chevronGO.GetComponent<Image>();
            var chevronSprite = Resources.Load<Sprite>("Sprites/Chevron");
            if (chevronSprite != null)
            {
                chevronImg.sprite = chevronSprite;
            }
            else
            {
                // Fallback: criar um triângulo simples
                Debug.LogWarning("Sprite 'Sprites/Chevron' não encontrado. Usando fallback.");
                chevronImg.color = ChevronColor;
            }

            chevronImg.color = ChevronColor;
            chevronImg.raycastTarget = false;

            return chevronRT;
        }

        /// <summary>
        /// Cria a lista de subcategorias para uma categoria
        /// </summary>
        /// <param name="category">Categoria pai</param>
        /// <returns>GameObject da lista de subcategorias</returns>
        private GameObject CreateSubcategoryList(Category category)
        {
            var subList = new GameObject($"SubList_{category.Id.ToString(CultureInfo.InvariantCulture)}", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            subList.transform.SetParent(_contentContainer, false);

            var subRT = subList.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0, 1);
            subRT.anchorMax = new Vector2(1, 1);
            subRT.pivot = new Vector2(0.5f, 1);

            var vlg = subList.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 0, 8, 8);
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandHeight = false;

            subList.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Criar itens de subcategoria
            foreach (string subcategory in category.Subcategories)
            {
                CreateSubcategoryItem(subList.transform, category.Id, subcategory);
            }

            return subList;
        }

        /// <summary>
        /// Cria um item de subcategoria
        /// </summary>
        /// <param name="parent">Transform pai</param>
        /// <param name="categoryId">ID da categoria pai</param>
        /// <param name="subcategoryName">Nome da subcategoria</param>
        private void CreateSubcategoryItem(Transform parent, long categoryId, string subcategoryName)
        {
            var subCard = UiKit.CreateCard(parent, new Vector2(0, subcategoryItemHeight),
                SubcategoryColor, 14f, glass: true);

            // Adicionar botão
            var btn = subCard.gameObject.GetComponent<Button>() ?? subCard.gameObject.AddComponent<Button>();
            btn.targetGraphic = subCard;

            // Configurar cores do botão
            var colors = btn.colors;
            colors.highlightedColor = new Color(1, 1, 1, 0.15f);
            colors.pressedColor = new Color(1, 1, 1, 0.25f);
            btn.colors = colors;

            // Adicionar texto
            var txt = UiKit.TMP(subCard.transform, subcategoryName, subcategoryFontSize,
                TextColor, TextAlignmentOptions.MidlineLeft);
            txt.enableWordWrapping = false;
            txt.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = new Vector2(20, 8);
            txt.rectTransform.offsetMax = new Vector2(-20, -8);
            txt.raycastTarget = false;

            // Configurar clique
            string capturedSubcategory = subcategoryName;
            btn.onClick.AddListener(() => ShowPiecesModal(categoryId, capturedSubcategory));

            subCard.name = $"Subcategory_{subcategoryName}";
        }

        /// <summary>
        /// Configura a interação do header da categoria
        /// </summary>
        /// <param name="header">GameObject do header</param>
        /// <param name="categoryId">ID da categoria</param>
        /// <param name="subList">GameObject da lista de subcategorias</param>
        /// <param name="chevron">RectTransform do chevron</param>
        private void SetupCategoryHeaderInteraction(GameObject header, long categoryId, GameObject subList, RectTransform chevron)
        {
            var btn = header.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                bool expanded = !_categoryExpanded[categoryId];
                _categoryExpanded[categoryId] = expanded;

                subList.SetActive(expanded);

                if (chevron != null)
                    chevron.localEulerAngles = expanded ? new Vector3(0, 0, 180) : Vector3.zero;

                // Forçar atualização do layout
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(_contentContainer);
            });
        }

        /// <summary>
        /// Exibe o modal com as peças da subcategoria selecionada
        /// </summary>
        /// <param name="categoryId">ID da categoria</param>
        /// <param name="subcategoryName">Nome da subcategoria</param>
        private void ShowPiecesModal(long categoryId, string subcategoryName)
        {
            CloseModal();

            if (_parentCanvas == null)
            {
                Debug.LogError("Canvas pai não definido para mostrar modal");
                return;
            }

            CreateModalOverlay();
            var modalPanel = CreateModalPanel();
            CreateModalHeader(modalPanel.transform, subcategoryName);
            var scrollContent = CreateModalScrollView(modalPanel.transform);
            PopulateModalWithPieces(scrollContent, categoryId);
            CreateModalFooter(modalPanel.transform, subcategoryName);
        }

        /// <summary>
        /// Cria o overlay do modal
        /// </summary>
        private void CreateModalOverlay()
        {
            var overlay = new GameObject("ModalOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
            overlay.transform.SetParent(_parentCanvas.transform, false);
            _openModal = overlay;

            var ovRT = overlay.GetComponent<RectTransform>();
            ovRT.anchorMin = Vector2.zero;
            ovRT.anchorMax = Vector2.one;
            ovRT.offsetMin = Vector2.zero;
            ovRT.offsetMax = Vector2.zero;

            overlay.GetComponent<Image>().color = ModalOverlayColor;
            overlay.GetComponent<Button>().onClick.AddListener(CloseModal);
        }

        /// <summary>
        /// Cria o painel principal do modal
        /// </summary>
        /// <returns>GameObject do painel do modal</returns>
        private GameObject CreateModalPanel()
        {
            var panel = new GameObject("ModalPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(_openModal.transform, false);

            var pRT = panel.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0.05f, 0.1f);
            pRT.anchorMax = new Vector2(0.95f, 0.9f);
            pRT.offsetMin = Vector2.zero;
            pRT.offsetMax = Vector2.zero;

            var pImg = panel.GetComponent<Image>();
            pImg.color = ModalPanelColor;

            var roundedSprite = Resources.Load<Sprite>("Sprites/RoundedPanel");
            if (roundedSprite != null)
            {
                pImg.sprite = roundedSprite;
                pImg.type = Image.Type.Sliced;
            }

            return panel;
        }

        /// <summary>
        /// Cria o header do modal
        /// </summary>
        /// <param name="parent">Transform pai</param>
        /// <param name="title">Título do modal</param>
        private void CreateModalHeader(Transform parent, string title)
        {
            var header = new GameObject("Header", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            header.transform.SetParent(parent, false);

            var hRT = header.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1);
            hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.offsetMin = new Vector2(16, -60);
            hRT.offsetMax = new Vector2(-16, -16);

            var hHLG = header.GetComponent<HorizontalLayoutGroup>();
            hHLG.childAlignment = TextAnchor.MiddleLeft;
            hHLG.spacing = 16;

            // Título
            var titleTMP = UiKit.TMP(header.transform, title, 24, ModalTextColor,
                TextAlignmentOptions.MidlineLeft, bold: true);

            // Spacer
            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(header.transform, false);
            spacer.GetComponent<LayoutElement>().flexibleWidth = 1;

            // Botão fechar
            var closeBtn = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtn.transform.SetParent(header.transform, false);
            var cRT = closeBtn.GetComponent<RectTransform>();
            cRT.sizeDelta = new Vector2(32, 32);
            closeBtn.GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f, 1f);
            closeBtn.GetComponent<Button>().onClick.AddListener(CloseModal);

            UiKit.TMP(closeBtn.transform, "✕", 18, ModalTextColor, TextAlignmentOptions.Center, bold: true);
        }

        /// <summary>
        /// Cria a scroll view do modal
        /// </summary>
        /// <param name="parent">Transform pai</param>
        /// <returns>Transform do conteúdo scrollável</returns>
        private Transform CreateModalScrollView(Transform parent)
        {
            var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGO.transform.SetParent(parent, false);

            var sRT = scrollGO.GetComponent<RectTransform>();
            sRT.anchorMin = Vector2.zero;
            sRT.anchorMax = Vector2.one;
            sRT.offsetMin = new Vector2(16, 16);
            sRT.offsetMax = new Vector2(-16, -76);

            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollGO.GetComponent<Image>().color = new Color(0, 0, 0, 0.05f);

            // Viewport
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vRT = viewport.GetComponent<RectTransform>();
            vRT.anchorMin = Vector2.zero;
            vRT.anchorMax = Vector2.one;
            vRT.offsetMin = Vector2.zero;
            vRT.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            scrollRect.viewport = vRT;

            // Content
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

        /// <summary>
        /// Popula o modal com as peças
        /// </summary>
        /// <param name="parent">Transform pai</param>
        /// <param name="categoryId">Categoria utilizada para filtrar as peças</param>
        private void PopulateModalWithPieces(Transform parent, long categoryId)
        {
            foreach (var piece in SampleData.Pieces)
            {
                if (piece.CategoryId != categoryId)
                {
                    continue;
                }

                var pieceCard = UiKit.CreateCard(parent, new Vector2(0, 56),
                    new Color(1f, 1f, 1f, 0.3f), 8f, glass: true);

                var btn = pieceCard.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    CloseModal();
                    OnPieceSelected?.Invoke(piece);
                });

                var pieceTMP = UiKit.TMP(pieceCard.transform, piece.Name, 18, ModalTextColor,
                    TextAlignmentOptions.MidlineLeft);
                pieceTMP.raycastTarget = false;
                var pieceRT = pieceTMP.rectTransform;
                pieceRT.anchorMin = Vector2.zero;
                pieceRT.anchorMax = Vector2.one;
                pieceRT.offsetMin = new Vector2(16, 8);
                pieceRT.offsetMax = new Vector2(-16, -8);

                pieceCard.name = $"PieceCard_{piece.Name}";
            }
        }

        /// <summary>
        /// Cria um rodapé com dicas adicionais no modal.
        /// </summary>
        private void CreateModalFooter(Transform parent, string subcategoryName)
        {
            var footerCard = UiKit.CreateCard(parent, new Vector2(0, 120f),
                new Color(1f, 1f, 1f, 0.15f), 12f, glass: true);

            var layout = footerCard.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 16, 16);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = footerCard.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            UiKit.TMP(footerCard.transform, "Como aproveitar", 20,
                ModalTextColor, TextAlignmentOptions.Left, bold: true).enableWordWrapping = true;

            var tipText =
                $"Explore as peças de {subcategoryName} para descobrir fichas técnicas detalhadas, fotos adicionais e curiosidades históricas. Toque novamente na categoria para voltar à lista principal.";

            var tipTMP = UiKit.TMP(footerCard.transform, tipText, 16,
                ModalTextColor, TextAlignmentOptions.Left);
            tipTMP.enableWordWrapping = true;

            var accessibility = UiKit.TMP(footerCard.transform,
                "Dica: em telas pequenas o modal ocupa toda a altura disponível, mantendo espaçamentos seguros para gestos de deslize.",
                14, ModalTextColor, TextAlignmentOptions.Left);
            accessibility.enableWordWrapping = true;
        }

        /// <summary>
        /// Fecha o modal ativo
        /// </summary>
        public void CloseModal()
        {
            if (_openModal != null)
            {
                Destroy(_openModal);
                _openModal = null;
            }
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
        /// Obtém referência ao GameObject da página
        /// </summary>
        /// <returns>GameObject da página ou null se não foi criada</returns>
        public GameObject GetPageObject()
        {
            return _pageObject;
        }

        /// <summary>
        /// Atualiza as configurações visuais da página
        /// </summary>
        /// <param name="headerHeight">Nova altura dos headers</param>
        /// <param name="itemHeight">Nova altura dos itens</param>
        /// <param name="titleSize">Novo tamanho da fonte dos títulos</param>
        /// <param name="itemSize">Novo tamanho da fonte dos itens</param>
        public void UpdateVisualSettings(float headerHeight, float itemHeight, int titleSize, int itemSize)
        {
            categoryHeaderHeight = headerHeight;
            subcategoryItemHeight = itemHeight;
            categoryTitleFontSize = titleSize;
            subcategoryFontSize = itemSize;
        }

        /// <summary>
        /// Expande ou recolhe uma categoria específica
        /// </summary>
        /// <param name="categoryId">ID da categoria</param>
        /// <param name="expand">True para expandir, false para recolher</param>
        public void SetCategoryExpanded(long categoryId, bool expand)
        {
            if (_categoryExpanded.ContainsKey(categoryId))
            {
                _categoryExpanded[categoryId] = expand;

                // Encontrar e atualizar a UI se necessário
                var subListGO = _contentContainer?.Find($"SubList_{categoryId.ToString(CultureInfo.InvariantCulture)}")?.gameObject;
                if (subListGO != null)
                {
                    subListGO.SetActive(expand);
                    Canvas.ForceUpdateCanvases();
                }
            }
        }

        /// <summary>
        /// Limpa recursos da página
        /// </summary>
        private void OnDestroy()
        {
            CloseModal();
            OnPieceSelected = null;
        }

        #region Editor Methods
#if UNITY_EDITOR
        /// <summary>
        /// Valida as configurações no editor
        /// </summary>
        private void OnValidate()
        {
            // Garantir que os valores sejam válidos
            categoryHeaderHeight = Mathf.Max(30f, categoryHeaderHeight);
            subcategoryItemHeight = Mathf.Max(20f, subcategoryItemHeight);
            categoryTitleFontSize = Mathf.Max(8, categoryTitleFontSize);
            subcategoryFontSize = Mathf.Max(8, subcategoryFontSize);
            infoCardMinimumHeight = Mathf.Max(80f, infoCardMinimumHeight);
        }
#endif
        #endregion
    }
}