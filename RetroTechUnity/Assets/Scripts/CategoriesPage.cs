using System;
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
        [SerializeField] private float categoryHeaderHeight = 80f; 
        [SerializeField] private float subcategoryItemHeight = 70f; 
        [SerializeField] private int categoryTitleFontSize = 36; 
        [SerializeField] private int subcategoryFontSize = 32; 

        // Colors
        private readonly Color HeaderColor = new Color32(255, 255, 255, 38);
        private readonly Color SubcategoryColor = new Color32(255, 255, 255, 20);
        private readonly Color TextColor = Color.white;
        private readonly Color ChevronColor = new Color32(120, 100, 170, 255);
        private readonly Color ModalOverlayColor = new Color(0f, 0f, 0f, 0.7f);
        private readonly Color ModalPanelColor = new Color32(42, 26, 72, 240);
        private readonly Color ModalPanelHighlightColor = new Color32(81, 52, 124, 255);
        private readonly Color ModalTextColor = new Color32(242, 240, 255, 255);
        private readonly Color PieceCardColor = new Color32(255, 255, 255, 28);
        private readonly Color MetadataChipColor = new Color32(106, 84, 158, 160);
        private readonly Color MetadataTextColor = new Color32(228, 220, 255, 255);
        private readonly Color SubtitleTextColor = new Color32(198, 192, 232, 255);

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
            // Criar título da página
            CreatePageTitle();

            foreach (Category category in SampleData.Categories)
            {
                CreateCategorySection(category);
            }
        }

        /// <summary>
        /// Cria o título da página de categorias
        /// </summary>
        private void CreatePageTitle()
        {
            var titleContainer = new GameObject("PageTitleContainer", typeof(RectTransform));
            titleContainer.transform.SetParent(_contentContainer, false);

            var titleRT = titleContainer.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.pivot = new Vector2(0.5f, 1);
            titleRT.sizeDelta = new Vector2(0, 120); // Altura maior do container

            // Criar texto do título
            var titleTMP = UiKit.TMP(titleContainer.transform, "Categorias", 56,
                TextColor, TextAlignmentOptions.Center, bold: true);

            titleTMP.rectTransform.anchorMin = Vector2.zero;
            titleTMP.rectTransform.anchorMax = Vector2.one;
            titleTMP.rectTransform.offsetMin = new Vector2(24, 24);
            titleTMP.rectTransform.offsetMax = new Vector2(-24, -24);
            titleTMP.raycastTarget = false;
            titleTMP.fontStyle = FontStyles.Bold;
            titleContainer.transform.SetAsFirstSibling(); // Colocar como primeiro elemento
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
            titleTMP.enableWordWrapping = true; // ALTERADO de false para true
            titleTMP.overflowMode = TMPro.TextOverflowModes.Ellipsis; // ALTERADO de Overflow para Ellipsis
            titleTMP.rectTransform.anchorMin = new Vector2(0, 0);
            titleTMP.rectTransform.anchorMax = new Vector2(1, 1);
            titleTMP.rectTransform.offsetMin = new Vector2(20, 12); // AUMENTADO padding de 16,8 para 20,12
            titleTMP.rectTransform.offsetMax = new Vector2(-60, -12); // AUMENTADO espaço para chevron
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
            chevronRT.sizeDelta = new Vector2(24, 24); // AUMENTADO de 18x18 para 24x24
            chevronRT.anchoredPosition = new Vector2(-18, 0); // AJUSTADO posição

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
            vlg.padding = new RectOffset(20, 20, 12, 12); // AUMENTADO padding de 16,0,8,8 para 20,20,12,12
            vlg.spacing = 10; // AUMENTADO de 8 para 10
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
            txt.enableWordWrapping = true; // ALTERADO de false para true
            txt.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = new Vector2(24, 12); // AUMENTADO padding de 20,8 para 24,12
            txt.rectTransform.offsetMax = new Vector2(-24, -12); // AUMENTADO padding
            txt.raycastTarget = false;

            // ADICIONADO: Garantir que o texto seja sempre visível
            txt.fontSizeMin = 20; // Tamanho mínimo da fonte
            txt.enableAutoSizing = false; // Desabilitar auto-sizing para manter tamanho consistente

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

            var pieces = GetPiecesForSubcategory(categoryId, subcategoryName);

            CreateModalOverlay();
            var modalPanel = CreateModalPanel();
            CreateModalHeader(modalPanel.transform, subcategoryName, pieces.Count);
            var scrollContent = CreateModalScrollView(modalPanel.transform);
            PopulateModalWithPieces(scrollContent, pieces, subcategoryName);
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
        /// Obtém as peças que pertencem à categoria e subcategoria selecionadas
        /// </summary>
        private List<ComputerPiece> GetPiecesForSubcategory(long categoryId, string subcategoryName)
        {
            var pieces = SampleData.Pieces?
                .Where(piece => piece.CategoryId == categoryId)
                .ToList() ?? new List<ComputerPiece>();

            if (pieces.Count == 0 || string.IsNullOrWhiteSpace(subcategoryName))
            {
                return pieces;
            }

            var filtered = pieces
                .Where(piece => !string.IsNullOrWhiteSpace(piece.Subcategory) &&
                                piece.Subcategory.Equals(subcategoryName, StringComparison.InvariantCultureIgnoreCase))
                .ToList();

            return filtered.Count > 0 ? filtered : pieces;
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

            var accent = new GameObject("PanelAccent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(panel.transform, false);

            var accentRT = accent.GetComponent<RectTransform>();
            accentRT.anchorMin = new Vector2(0f, 1f);
            accentRT.anchorMax = new Vector2(1f, 1f);
            accentRT.pivot = new Vector2(0.5f, 1f);
            accentRT.sizeDelta = new Vector2(0f, 140f);
            accentRT.anchoredPosition = Vector2.zero;

            var accentImg = accent.GetComponent<Image>();
            accentImg.color = ModalPanelHighlightColor;
            accentImg.raycastTarget = false;

            accent.transform.SetAsFirstSibling();

            return panel;
        }

        /// <summary>
        /// Cria o header do modal
        /// </summary>
        /// <param name="parent">Transform pai</param>
        /// <param name="title">Título do modal</param>
        /// <param name="pieceCount">Quantidade de peças exibidas</param>
        private void CreateModalHeader(Transform parent, string title, int pieceCount)
        {
            var header = new GameObject("Header", typeof(RectTransform), typeof(VerticalLayoutGroup));
            header.transform.SetParent(parent, false);

            var hRT = header.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1);
            hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.offsetMin = new Vector2(16, -140);
            hRT.offsetMax = new Vector2(-16, -16);

            var headerLayout = header.GetComponent<VerticalLayoutGroup>();
            headerLayout.childAlignment = TextAnchor.UpperLeft;
            headerLayout.spacing = 12;
            headerLayout.childControlWidth = true;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childControlHeight = true;
            headerLayout.childForceExpandHeight = false;

            var topRow = new GameObject("TopRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            topRow.transform.SetParent(header.transform, false);

            var topRowLayout = topRow.GetComponent<HorizontalLayoutGroup>();
            topRowLayout.childAlignment = TextAnchor.MiddleLeft;
            topRowLayout.spacing = 12;
            topRowLayout.childControlWidth = true;
            topRowLayout.childForceExpandWidth = false;
            topRowLayout.childControlHeight = false;
            topRowLayout.childForceExpandHeight = false;

            var titleTMP = UiKit.TMP(topRow.transform, title, 30, ModalTextColor,
                TextAlignmentOptions.Left, bold: true);
            titleTMP.enableWordWrapping = true;
            titleTMP.margin = Vector4.zero;

            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(topRow.transform, false);
            spacer.GetComponent<LayoutElement>().flexibleWidth = 1;

            var closeBtn = new GameObject("CloseBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtn.transform.SetParent(topRow.transform, false);
            var cRT = closeBtn.GetComponent<RectTransform>();
            cRT.sizeDelta = new Vector2(40, 40);

            var closeImg = closeBtn.GetComponent<Image>();
            closeImg.color = new Color32(92, 70, 142, 220);

            var btn = closeBtn.GetComponent<Button>();
            btn.onClick.AddListener(CloseModal);
            var btnColors = btn.colors;
            btnColors.highlightedColor = new Color32(118, 90, 176, 240);
            btnColors.pressedColor = new Color32(66, 44, 120, 255);
            btn.colors = btnColors;

            var closeTMP = UiKit.TMP(closeBtn.transform, "✕", 26, ModalTextColor, TextAlignmentOptions.Center, bold: true);
            closeTMP.raycastTarget = false;

            string pieceCountText = pieceCount == 1
                ? "1 peça disponível"
                : $"{pieceCount} peças disponíveis";
            var subtitleTMP = UiKit.TMP(header.transform, pieceCountText, 20, SubtitleTextColor, TextAlignmentOptions.Left);
            subtitleTMP.enableWordWrapping = true;
            subtitleTMP.margin = new Vector4(0, 0, 0, 4);
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
            sRT.offsetMax = new Vector2(-16, -160);

            var scrollRect = scrollGO.GetComponent<ScrollRect>();
            scrollRect.vertical = true;
            scrollRect.horizontal = false;
            scrollGO.GetComponent<Image>().color = new Color32(255, 255, 255, 18);

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
        /// <param name="pieces">Coleção de peças a serem exibidas</param>
        /// <param name="subcategoryName">Nome da subcategoria ativa</param>
        private void PopulateModalWithPieces(Transform parent, List<ComputerPiece> pieces, string subcategoryName)
        {
            if (pieces == null || pieces.Count == 0)
            {
                string emptyMessage = string.IsNullOrWhiteSpace(subcategoryName)
                    ? "Nenhuma peça disponível nesta categoria no momento."
                    : $"Nenhuma peça cadastrada para \"{subcategoryName}\" no momento.";

                var emptyTMP = UiKit.TMP(parent, emptyMessage, 22, ModalTextColor, TextAlignmentOptions.Center);
                emptyTMP.alignment = TextAlignmentOptions.Center;
                emptyTMP.margin = new Vector4(0, 48, 0, 0);
                emptyTMP.enableWordWrapping = true;
                return;
            }

            foreach (var piece in pieces)
            {
                var pieceCard = UiKit.CreateCard(parent, new Vector2(0, 0),
                    PieceCardColor, 18f, glass: true);

                var layoutElement = pieceCard.GetComponent<LayoutElement>();
                if (layoutElement != null)
                {
                    layoutElement.minHeight = 0f;
                    layoutElement.preferredHeight = -1f;
                    layoutElement.flexibleHeight = 0f;
                }

                var cardLayout = pieceCard.gameObject.GetComponent<VerticalLayoutGroup>() ??
                                  pieceCard.gameObject.AddComponent<VerticalLayoutGroup>();
                cardLayout.padding = new RectOffset(20, 20, 20, 20);
                cardLayout.spacing = 12;
                cardLayout.childAlignment = TextAnchor.UpperLeft;
                cardLayout.childControlWidth = true;
                cardLayout.childForceExpandWidth = true;
                cardLayout.childControlHeight = true;
                cardLayout.childForceExpandHeight = false;

                var cardFitter = pieceCard.gameObject.GetComponent<ContentSizeFitter>() ??
                                 pieceCard.gameObject.AddComponent<ContentSizeFitter>();
                cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

                var titleTMP = UiKit.TMP(pieceCard.transform, piece.Name, 26, ModalTextColor,
                    TextAlignmentOptions.Left, bold: true);
                titleTMP.enableWordWrapping = true;
                titleTMP.margin = new Vector4(0, 0, 0, 4);

                var metadataValues = new List<string>();
                if (piece.YearManufactured > 0)
                    metadataValues.Add(piece.YearManufactured.ToString(CultureInfo.InvariantCulture));
                if (!string.IsNullOrWhiteSpace(piece.Manufacturer))
                    metadataValues.Add(piece.Manufacturer);
                if (!string.IsNullOrWhiteSpace(piece.Subcategory))
                    metadataValues.Add(piece.Subcategory);

                if (metadataValues.Count > 0)
                {
                    var metadataRow = new GameObject("MetadataRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                    metadataRow.transform.SetParent(pieceCard.transform, false);

                    var metadataLayout = metadataRow.GetComponent<HorizontalLayoutGroup>();
                    metadataLayout.spacing = 8;
                    metadataLayout.childAlignment = TextAnchor.MiddleLeft;
                    metadataLayout.childControlWidth = false;
                    metadataLayout.childForceExpandWidth = false;
                    metadataLayout.childControlHeight = false;
                    metadataLayout.childForceExpandHeight = false;

                    foreach (var value in metadataValues)
                    {
                        CreateMetadataChip(metadataRow.transform, value);
                    }
                }

                var description = string.IsNullOrWhiteSpace(piece.Description)
                    ? "Detalhes não disponíveis."
                    : piece.Description;
                var descTMP = UiKit.TMP(pieceCard.transform, description, 20, new Color32(210, 205, 240, 255),
                    TextAlignmentOptions.Left);
                descTMP.enableWordWrapping = true;
                descTMP.margin = new Vector4(0, 0, 0, 12);

                CreatePieceDetailsButton(pieceCard.transform, piece);

                pieceCard.gameObject.name = $"PieceCard_{piece.Name}";
            }
        }

        /// <summary>
        /// Cria um chip de metadado (ano, fabricante, etc.)
        /// </summary>
        private void CreateMetadataChip(Transform parent, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var chip = new GameObject("MetadataChip", typeof(RectTransform), typeof(Image));
            chip.transform.SetParent(parent, false);

            var chipImg = chip.GetComponent<Image>();
            chipImg.color = MetadataChipColor;

            var layout = chip.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 6, 6);
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var fitter = chip.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var textTMP = UiKit.TMP(chip.transform, value, 18, MetadataTextColor, TextAlignmentOptions.MidlineLeft);
            textTMP.enableWordWrapping = false;
            textTMP.raycastTarget = false;

            var chipLayoutElement = chip.AddComponent<LayoutElement>();
            chipLayoutElement.minHeight = 32f;
            chipLayoutElement.preferredHeight = 32f;
        }

        /// <summary>
        /// Cria o botão de detalhes de cada peça
        /// </summary>
        private void CreatePieceDetailsButton(Transform parent, ComputerPiece piece)
        {
            var buttonGO = new GameObject("DetailsButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);

            var btnImage = buttonGO.GetComponent<Image>();
            btnImage.color = new Color32(114, 74, 160, 255);

            var btnLayout = buttonGO.AddComponent<HorizontalLayoutGroup>();
            btnLayout.padding = new RectOffset(24, 24, 8, 8);
            btnLayout.childAlignment = TextAnchor.MiddleCenter;
            btnLayout.childControlWidth = true;
            btnLayout.childForceExpandWidth = true;
            btnLayout.childControlHeight = true;
            btnLayout.childForceExpandHeight = false;

            var layoutElement = buttonGO.AddComponent<LayoutElement>();
            layoutElement.minHeight = 52f;
            layoutElement.preferredHeight = 52f;
            layoutElement.flexibleWidth = 1f;

            var buttonText = UiKit.TMP(buttonGO.transform, "Ver detalhes", 20, Color.white, TextAlignmentOptions.Center, bold: true);
            buttonText.raycastTarget = false;

            var button = buttonGO.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color32(134, 94, 190, 255);
            colors.pressedColor = new Color32(94, 54, 150, 255);
            button.colors = colors;

            button.onClick.AddListener(() =>
            {
                CloseModal();
                OnPieceSelected?.Invoke(piece);
            });
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
        }
#endif
        #endregion
    }
}