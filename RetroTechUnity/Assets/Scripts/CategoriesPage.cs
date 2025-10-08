using System.Collections.Generic;
using System.Globalization;
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
        [SerializeField] private float categoryHeaderHeight = 68f;
        [SerializeField] private float subcategoryItemHeight = 56f;
        [SerializeField] private int categoryTitleFontSize = 26;
        [SerializeField] private int subcategoryFontSize = 20;

        // Colors
        private readonly Color HeaderGradientTop = new Color32(255, 255, 255, 70);
        private readonly Color HeaderGradientBottom = new Color32(255, 255, 255, 25);
        private readonly Color PanelGradientTop = new Color32(189, 164, 255, 255);
        private readonly Color PanelGradientBottom = new Color32(255, 124, 208, 255);
        private readonly Color SubcategoryColor = new Color32(255, 255, 255, 38);
        private readonly Color PageTitleColor = new Color32(248, 244, 255, 255);
        private readonly Color TextColor = new Color32(252, 247, 255, 255);
        private readonly Color ChevronColor = new Color32(255, 255, 255, 255);
        private readonly Color ModalOverlayColor = new Color(0, 0, 0, 0.7f);
        private readonly Color ModalPanelColor = new Color(1f, 1f, 1f, 0.95f);
        private readonly Color ModalTextColor = new Color32(50, 50, 70, 255);

        // Events
        public System.Action<ComputerPiece> OnPieceSelected;

        // State
        private readonly Dictionary<long, bool> _categoryExpanded = new Dictionary<long, bool>();
        private GameObject _pageObject;
        private RectTransform _contentContainer;
        private RectTransform _categoryListContainer;
        private Canvas _parentCanvas;
        private GameObject _openModal;
        private Sprite _panelBackgroundSprite;
        private Sprite _headerBackgroundSprite;

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

            ConfigureContentLayout();
            CreatePageHeader();
            _categoryListContainer = CreateCategoriesPanel();
            CreateCategoriesContent();

            return _pageObject;
        }

        /// <summary>
        /// Ajusta o layout padrão recebido da superfície para combinar com o protótipo.
        /// </summary>
        private void ConfigureContentLayout()
        {
            if (_contentContainer == null)
            {
                return;
            }

            var layout = _contentContainer.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.padding = new RectOffset(32, 32, 52, 52);
                layout.spacing = 28f;
                layout.childAlignment = TextAnchor.UpperLeft;
            }
        }

        /// <summary>
        /// Cria o cabeçalho principal da página com o título "RetroTech".
        /// </summary>
        private void CreatePageHeader()
        {
            var title = UiKit.TMP(_contentContainer, "RetroTech", 48, PageTitleColor,
                TextAlignmentOptions.Left, bold: true);
            title.name = "PageTitle";
            title.enableWordWrapping = false;

            var rect = title.rectTransform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(8, 0);
            rect.offsetMax = new Vector2(-8, 0);

            var layout = title.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 64f;
        }

        /// <summary>
        /// Cria o painel que envolve a lista de categorias, aplicando gradiente e sombra.
        /// </summary>
        /// <returns>RectTransform do painel para adicionar as categorias.</returns>
        private RectTransform CreateCategoriesPanel()
        {
            var panelGO = new GameObject("CategoriesPanel", typeof(RectTransform), typeof(Image),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            panelGO.transform.SetParent(_contentContainer, false);

            var panelRT = panelGO.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0, 1);
            panelRT.anchorMax = new Vector2(1, 1);
            panelRT.pivot = new Vector2(0.5f, 1f);
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            var panelImage = panelGO.GetComponent<Image>();
            panelImage.sprite = EnsureRoundedGradient(ref _panelBackgroundSprite, PanelGradientTop, PanelGradientBottom, 46f, 512, 1536);
            panelImage.type = Image.Type.Simple;
            panelImage.color = Color.white;

            var panelShadow = panelGO.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(0f, 0f, 0f, 0.25f);
            panelShadow.effectDistance = new Vector2(0f, 12f);
            panelShadow.useGraphicAlpha = true;

            var layout = panelGO.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 32, 32);
            layout.spacing = 18f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            panelGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            return panelRT;
        }

        /// <summary>
        /// Cria todo o conteúdo das categorias
        /// </summary>
        private void CreateCategoriesContent()
        {
            if (_categoryListContainer == null)
            {
                return;
            }

            foreach (Category category in SampleData.Categories)
            {
                CreateCategorySection(category);
            }
        }

        /// <summary>
        /// Cria uma seção completa de categoria com header e subcategorias
        /// </summary>
        /// <param name="category">Categoria a ser criada</param>
        private void CreateCategorySection(Category category)
        {
            // Criar header da categoria
            var (header, chevron) = CreateCategoryHeader(_categoryListContainer, category.Name);

            // Criar lista de subcategorias
            var subList = CreateSubcategoryList(_categoryListContainer, category);

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
        private (GameObject header, RectTransform chevron) CreateCategoryHeader(Transform parent, string categoryName)
        {
            var headerCard = UiKit.CreateCard(parent, new Vector2(0, categoryHeaderHeight),
                Color.white, 26f, glass: true);
            var header = headerCard.gameObject;

            headerCard.sprite = EnsureRoundedGradient(ref _headerBackgroundSprite, HeaderGradientTop, HeaderGradientBottom, 26f);
            headerCard.type = Image.Type.Simple;
            headerCard.color = Color.white;

            var shadow = header.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.2f);
            shadow.effectDistance = new Vector2(0f, 6f);
            shadow.useGraphicAlpha = true;

            // Adicionar botão ao header
            var btn = header.GetComponent<Button>() ?? header.AddComponent<Button>();
            btn.targetGraphic = headerCard;

            // Configurar cores do botão
            var colors = btn.colors;
            colors.highlightedColor = new Color(1, 1, 1, 0.15f);
            colors.pressedColor = new Color(1, 1, 1, 0.25f);
            btn.colors = colors;

            // Adicionar título
            var titleTMP = UiKit.TMP(header.transform, categoryName, categoryTitleFontSize,
                TextColor, TextAlignmentOptions.MidlineLeft, bold: true);
            titleTMP.enableWordWrapping = false;
            titleTMP.overflowMode = TMPro.TextOverflowModes.Overflow;
            titleTMP.rectTransform.anchorMin = new Vector2(0, 0);
            titleTMP.rectTransform.anchorMax = new Vector2(1, 1);
            titleTMP.rectTransform.offsetMin = new Vector2(24, 10);
            titleTMP.rectTransform.offsetMax = new Vector2(-56, -10);
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
        private GameObject CreateSubcategoryList(Transform parent, Category category)
        {
            var subList = new GameObject($"SubList_{category.Id.ToString(CultureInfo.InvariantCulture)}", typeof(RectTransform),
                typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            subList.transform.SetParent(parent, false);

            var subRT = subList.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0, 1);
            subRT.anchorMax = new Vector2(1, 1);
            subRT.pivot = new Vector2(0.5f, 1);
            subRT.offsetMin = Vector2.zero;
            subRT.offsetMax = Vector2.zero;

            var vlg = subList.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 16, 6, 12);
            vlg.spacing = 12;
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
                SubcategoryColor, 20f, glass: true);

            // Adicionar botão
            var btn = subCard.gameObject.GetComponent<Button>() ?? subCard.gameObject.AddComponent<Button>();
            btn.targetGraphic = subCard;

            // Configurar cores do botão
            var colors = btn.colors;
            colors.highlightedColor = new Color(1, 1, 1, 0.2f);
            colors.pressedColor = new Color(1, 1, 1, 0.3f);
            btn.colors = colors;

            // Adicionar texto
            var txt = UiKit.TMP(subCard.transform, subcategoryName, subcategoryFontSize,
                TextColor, TextAlignmentOptions.MidlineLeft);
            txt.enableWordWrapping = false;
            txt.overflowMode = TMPro.TextOverflowModes.Ellipsis;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = new Vector2(20, 10);
            txt.rectTransform.offsetMax = new Vector2(-20, -10);
            txt.raycastTarget = false;

            // Configurar clique
            string capturedSubcategory = subcategoryName;
            btn.onClick.AddListener(() => ShowPiecesModal(categoryId, capturedSubcategory));

            subCard.name = $"Subcategory_{subcategoryName}";
        }

        /// <summary>
        /// Cria (ou reutiliza) um sprite de gradiente arredondado.
        /// </summary>
        /// <param name="cache">Referência para armazenar o sprite gerado.</param>
        /// <param name="top">Cor do topo do gradiente.</param>
        /// <param name="bottom">Cor da base do gradiente.</param>
        /// <param name="radius">Raio da borda arredondada.</param>
        /// <param name="width">Largura da textura gerada.</param>
        /// <param name="height">Altura da textura gerada.</param>
        /// <returns>Sprite com gradiente vertical e cantos arredondados.</returns>
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
        /// Gera dinamicamente uma textura com gradiente vertical e cantos arredondados.
        /// </summary>
        /// <param name="top">Cor do topo do gradiente.</param>
        /// <param name="bottom">Cor da base do gradiente.</param>
        /// <param name="radius">Raio das bordas arredondadas.</param>
        /// <param name="width">Largura da textura gerada.</param>
        /// <param name="height">Altura da textura gerada.</param>
        /// <returns>Sprite configurado com o gradiente.</returns>
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
                var searchRoot = (Transform)_categoryListContainer ?? _contentContainer;
                var subListGO = searchRoot?.Find($"SubList_{categoryId.ToString(CultureInfo.InvariantCulture)}")?.gameObject;
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