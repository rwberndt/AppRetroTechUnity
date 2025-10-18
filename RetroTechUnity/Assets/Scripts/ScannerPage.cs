using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Globalization;
#if ZXING_PRESENT
using ZXing;
using ZXing.Common;
#endif
using static RetroTech.UiKit;

namespace RetroTech
{
    /// <summary>
    /// Página do Scanner QR do RetroTech que permite escanear códigos QR
    /// para acessar rapidamente informações de peças do museu.
    /// Versão corrigida com renderização adequada dos elementos UI.
    /// </summary>
    public class ScannerPage : MonoBehaviour
    {
        [Header("Page Configuration")]
        [SerializeField] private string pageTitle = "ScannerPage";

        [Header("Visual Configuration")]
        [SerializeField] private float previewCardHeight = 280f;
        [SerializeField] private float tipsCardHeight = 140f;
        [SerializeField] private float qrFrameSize = 180f;
        [SerializeField] private int titleFontSize = 36;
        [SerializeField] private int scanTitleFontSize = 24;
        [SerializeField] private int scanDescFontSize = 18;
        [SerializeField] private int tipsTitleFontSize = 20;
        [SerializeField] private int tipsFontSize = 16;

        // Events
        public System.Action<ComputerPiece> OnPieceScanned;

        // State
        private GameObject _pageObject;
        private RectTransform _contentContainer;
        private Canvas _parentCanvas;
        private Button _scanButton;
        private bool _isScanning = false;
        private bool _pageInitialized = false;

        // Cached responsive references
        private LayoutElement _previewCardLayout;
        private LayoutElement _tipsCardLayout;
        private LayoutElement _qrFrameLayout;
        private RectTransform _qrFrameRect;

#if ZXING_PRESENT
        private WebCamTexture _webcam;
        private BarcodeReader _qrReader;
        private Coroutine _scanCoroutine;
#endif

        /// <summary>
        /// Cria e configura a página do scanner
        /// </summary>
        public GameObject CreatePage(Transform parent, Canvas canvas, System.Func<string, (GameObject surface, RectTransform content)> buildSurfaceFunc)
        {
            _parentCanvas = canvas;
            var (surface, content) = buildSurfaceFunc(pageTitle);
            _pageObject = surface.gameObject;
            _contentContainer = content;

            InitializeQRReader();
            CreateScannerContent();

            _pageInitialized = true;
            ApplyResponsiveMetrics();

            return _pageObject;
        }

        /// <summary>
        /// Inicializa o leitor de QR code
        /// </summary>
        private void InitializeQRReader()
        {
#if ZXING_PRESENT
            _qrReader = new BarcodeReader 
            { 
                AutoRotate = true, 
                Options = new DecodingOptions { TryHarder = true } 
            };
#endif
        }

        /// <summary>
        /// Cria todo o conteúdo da página do scanner
        /// </summary>
        private void CreateScannerContent()
        {
            CreateTitle();
            CreatePreviewArea();
            AddSpacer(_contentContainer, 24);
            CreateScanButton();
            AddSpacer(_contentContainer, 24);
            CreateTipsCard();
            ApplyResponsiveMetrics();
        }

        /// <summary>
        /// Cria o título da página
        /// </summary>
        private void CreateTitle()
        {
            var titleTMP = UiKit.TMP(_contentContainer, "Scanner QR", titleFontSize,
                Color.white, TextAlignmentOptions.Left, bold: true);
            titleTMP.margin = new Vector4(0, 0, 0, 32);
        }

        /// <summary>
        /// Cria a área de preview do QR Code
        /// </summary>
        private void CreatePreviewArea()
        {
            float responsivePreviewHeight = GetResponsivePreviewHeight();

            // Usar o mesmo padrão do GameManager para manter consistência
            var previewCard = CreateGlassCard(_contentContainer, responsivePreviewHeight);
            _previewCardLayout = previewCard.GetComponent<LayoutElement>();
            if (_previewCardLayout != null)
            {
                _previewCardLayout.flexibleHeight = 0.5f;
            }

            var previewVLG = previewCard.gameObject.AddComponent<VerticalLayoutGroup>();
            previewVLG.childAlignment = TextAnchor.MiddleCenter;
            int previewSpacing = Mathf.RoundToInt(ResponsiveTypography.ResponsiveSpacing(16f));
            previewVLG.spacing = previewSpacing;
            int previewPadding = Mathf.RoundToInt(ResponsiveTypography.ResponsiveSpacing(24f));
            previewVLG.padding = new RectOffset(previewPadding, previewPadding, previewPadding, previewPadding);
            previewVLG.childControlWidth = true;
            previewVLG.childForceExpandWidth = true;
            previewVLG.childControlHeight = true;
            previewVLG.childForceExpandHeight = false;

            // QR Code frame
            CreateQRFrame(previewCard.transform);

            // Title
            var scanTitleTMP = UiKit.TMP(previewCard.transform, "Scanner QR Code",
                scanTitleFontSize, Color.white, TextAlignmentOptions.Center, bold: true);
            scanTitleTMP.enableWordWrapping = false;

            // Description
            var scanDescTMP = UiKit.TMP(previewCard.transform,
                "Aponte a câmera para o QR code de uma peça para ver seus detalhes.",
                scanDescFontSize, new Color32(255, 255, 255, 200), TextAlignmentOptions.Center);
            scanDescTMP.enableWordWrapping = true;
            scanDescTMP.margin = new Vector4(8, 0, 8, 0);
        }

        /// <summary>
        /// Cria o frame do QR Code
        /// </summary>
        private GameObject CreateQRFrame(Transform parent)
        {
            var frameGO = new GameObject("QRFrame", typeof(RectTransform), typeof(Image));
            frameGO.transform.SetParent(parent, false);

            var frameRT = frameGO.GetComponent<RectTransform>();
            float responsiveFrame = GetResponsiveFrameSize();
            frameRT.sizeDelta = new Vector2(responsiveFrame, responsiveFrame);
            _qrFrameRect = frameRT;

            var frameImg = frameGO.GetComponent<Image>();
            frameImg.color = new Color(1f, 1f, 1f, 0.2f);
            frameImg.raycastTarget = false;

            // Tentar carregar sprite arredondado, caso contrário usar cor sólida
            var frameSprite = Resources.Load<Sprite>("Sprites/RoundedPanel");
            if (frameSprite != null)
            {
                frameImg.sprite = frameSprite;
                frameImg.type = Image.Type.Sliced;
            }

            var frameLE = frameGO.AddComponent<LayoutElement>();
            frameLE.preferredWidth = responsiveFrame;
            frameLE.preferredHeight = responsiveFrame;
            frameLE.minWidth = responsiveFrame;
            frameLE.minHeight = responsiveFrame;
            frameLE.flexibleWidth = 0f;
            frameLE.flexibleHeight = 0f;
            _qrFrameLayout = frameLE;

            var fitter = frameGO.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            fitter.aspectRatio = 1f;

            // Ícone QR Code interno
            CreateQRIcon(frameGO.transform);

            return frameGO;
        }

        /// <summary>
        /// Cria o ícone interno do QR Code
        /// </summary>
        private void CreateQRIcon(Transform parent)
        {
            var qrIconGO = new GameObject("QRIcon", typeof(RectTransform), typeof(Image));
            qrIconGO.transform.SetParent(parent, false);

            var qrIconRT = qrIconGO.GetComponent<RectTransform>();
            qrIconRT.anchorMin = new Vector2(0.5f, 0.5f);
            qrIconRT.anchorMax = new Vector2(0.5f, 0.5f);
            qrIconRT.pivot = new Vector2(0.5f, 0.5f);
            qrIconRT.sizeDelta = new Vector2(80, 80);

            var qrIconImg = qrIconGO.GetComponent<Image>();
            qrIconImg.raycastTarget = false;

            // Tentar carregar sprite do QR, fallback para símbolo Unicode
            var qrSprite = Resources.Load<Sprite>("Sprites/qr_glyph");
            if (qrSprite != null)
            {
                qrIconImg.sprite = qrSprite;
                qrIconImg.color = Color.white;
            }
            else
            {
                // Usar cor transparente e adicionar texto
                qrIconImg.color = new Color(0, 0, 0, 0);

                // Criar texto do QR Code usando TextMeshPro
                var qrTextGO = new GameObject("QRText", typeof(RectTransform));
                qrTextGO.transform.SetParent(qrIconGO.transform, false);

                var qrTextTMP = UiKit.TMP(qrTextGO.transform, "⊞", 48,
                    Color.white, TextAlignmentOptions.Center);
                qrTextTMP.raycastTarget = false;
                qrTextTMP.enableWordWrapping = false;

                var qrTextRT = qrTextTMP.rectTransform;
                qrTextRT.anchorMin = Vector2.zero;
                qrTextRT.anchorMax = Vector2.one;
                qrTextRT.offsetMin = Vector2.zero;
                qrTextRT.offsetMax = Vector2.zero;
            }
        }

        /// <summary>
        /// Cria o botão de iniciar scanner
        /// </summary>
        private void CreateScanButton()
        {
            var scanButtonGO = CreatePrototypeCTAButton(_contentContainer.transform,
                "⚡ Iniciar Scanner", StartScan);
            _scanButton = scanButtonGO.GetComponent<Button>();
        }

        /// <summary>
        /// Cria o cartão de dicas
        /// </summary>
        private void CreateTipsCard()
        {
            float responsiveTipsHeight = GetResponsiveTipsHeight();
            var tipsCard = CreateGlassCard(_contentContainer, responsiveTipsHeight);
            _tipsCardLayout = tipsCard.GetComponent<LayoutElement>();
            if (_tipsCardLayout != null)
            {
                _tipsCardLayout.flexibleHeight = 0.2f;
            }

            var tipsVLG = tipsCard.gameObject.AddComponent<VerticalLayoutGroup>();
            tipsVLG.childAlignment = TextAnchor.UpperLeft;
            int tipsSpacing = Mathf.RoundToInt(ResponsiveTypography.ResponsiveSpacing(8f));
            tipsVLG.spacing = tipsSpacing;
            int horizontalPadding = Mathf.RoundToInt(ResponsiveTypography.ResponsiveSpacing(20f));
            int verticalPadding = Mathf.RoundToInt(ResponsiveTypography.ResponsiveSpacing(16f));
            tipsVLG.padding = new RectOffset(horizontalPadding, horizontalPadding, verticalPadding, verticalPadding);
            tipsVLG.childControlWidth = true;
            tipsVLG.childForceExpandWidth = true;
            tipsVLG.childControlHeight = true;
            tipsVLG.childForceExpandHeight = false;

            var tipsTitleTMP = UiKit.TMP(tipsCard.transform, "Dicas para escanear:",
                tipsTitleFontSize, Color.white, bold: true);
            tipsTitleTMP.enableWordWrapping = false;
            tipsTitleTMP.margin = new Vector4(4, 0, 4, 0);

            var tipsTMP = UiKit.TMP(tipsCard.transform,
                "• Mantenha o QR code bem iluminado\n" +
                "• Mantenha a câmera estável\n" +
                "• Certifique-se que o código esteja completo na tela",
                tipsFontSize, new Color32(255, 255, 255, 180), TextAlignmentOptions.Left);
            tipsTMP.enableWordWrapping = true;
            tipsTMP.margin = new Vector4(4, 0, 4, 0);
        }

        /// <summary>
        /// Inicia o processo de escaneamento
        /// </summary>
        private void StartScan()
        {
            if (_isScanning) return;

            _isScanning = true;
            UpdateScanButtonText("Escaneando...");

            // Solicitar permissão de câmera no Android
            RequestCameraPermission();

#if ZXING_PRESENT
            _scanCoroutine = StartCoroutine(ScanQRCode());
#else
            // Simulação quando ZXing não está disponível
            StartCoroutine(SimulateScan());
#endif
        }

        /// <summary>
        /// Para o processo de escaneamento
        /// </summary>
        public void StopScan()
        {
            if (!_isScanning) return;

            _isScanning = false;
            UpdateScanButtonText("⚡ Iniciar Scanner");

#if ZXING_PRESENT
            if (_scanCoroutine != null)
            {
                StopCoroutine(_scanCoroutine);
                _scanCoroutine = null;
            }

            if (_webcam != null && _webcam.isPlaying)
            {
                _webcam.Stop();
                _webcam = null;
            }
#endif
        }

        /// <summary>
        /// Solicita permissão de câmera no Android
        /// </summary>
        private void RequestCameraPermission()
        {
#if UNITY_ANDROID
            try
            {
                if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                {
                    UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Erro ao solicitar permissão de câmera: {ex.Message}");
            }
#endif
        }

        /// <summary>
        /// Atualiza o texto do botão de scan
        /// </summary>
        private void UpdateScanButtonText(string text)
        {
            if (_scanButton != null)
            {
                var buttonText = _scanButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    buttonText.text = text;
                }
            }
        }

#if ZXING_PRESENT
        /// <summary>
        /// Corrotina para escanear QR Code usando câmera real
        /// </summary>
        private IEnumerator ScanQRCode()
        {
            // Verificar se há câmeras disponíveis
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogWarning("Nenhuma câmera encontrada. Simulando scan...");
                yield return StartCoroutine(SimulateScan());
                yield break;
            }

            // Inicializar câmera
            var device = WebCamTexture.devices[0];
            _webcam = new WebCamTexture(device.name);
            _webcam.Play();

            // Aguardar câmera inicializar
            yield return new WaitForSeconds(0.5f);

            if (!_webcam.isPlaying)
            {
                Debug.LogWarning("Câmera não pôde ser inicializada. Simulando scan...");
                yield return StartCoroutine(SimulateScan());
                yield break;
            }

            ComputerPiece foundPiece = null;
            float scanTimeout = 30f;
            float scanStartTime = Time.time;

            // Loop de escaneamento
            while (_isScanning && foundPiece == null && (Time.time - scanStartTime) < scanTimeout)
            {
                try
                {
                    if (_webcam.width > 16 && _webcam.height > 16)
                    {
                        var result = _qrReader.Decode(_webcam.GetPixels32(), _webcam.width, _webcam.height);
                        if (result != null)
                        {
                            foundPiece = FindPieceFromQRData(result.Text);
                            if (foundPiece != null)
                            {
                                Debug.Log($"QR Code encontrado: {result.Text}");
                                break;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Erro durante escaneamento: {ex.Message}");
                }

                yield return new WaitForSeconds(0.1f);
            }

            // Parar câmera
            if (_webcam != null && _webcam.isPlaying)
            {
                _webcam.Stop();
                _webcam = null;
            }

            _isScanning = false;
            UpdateScanButtonText("⚡ Iniciar Scanner");

            // Processar resultado
            if (foundPiece != null)
            {
                OnPieceScanned?.Invoke(foundPiece);
            }
            else
            {
                Debug.Log("Nenhum QR válido encontrado. Simulando resultado...");
                yield return StartCoroutine(SimulateScan());
            }
        }
#endif

        /// <summary>
        /// Simula um escaneamento para demonstração
        /// </summary>
        private IEnumerator SimulateScan()
        {
            // Simular tempo de escaneamento
            yield return new WaitForSeconds(2f);

            _isScanning = false;
            UpdateScanButtonText("⚡ Iniciar Scanner");

            // Selecionar peça aleatória para demonstração
            if (SampleData.Pieces.Count > 0)
            {
                int randomIndex = Random.Range(0, SampleData.Pieces.Count);
                var randomPiece = SampleData.Pieces[randomIndex];

                Debug.Log($"Simulando scan da peça: {randomPiece.Name}");
                OnPieceScanned?.Invoke(randomPiece);
            }
            else
            {
                Debug.LogWarning("Nenhuma peça disponível para simulação");
            }
        }

        /// <summary>
        /// Encontra uma peça baseada nos dados do QR Code
        /// </summary>
        private ComputerPiece FindPieceFromQRData(string qrData)
        {
            if (string.IsNullOrWhiteSpace(qrData))
            {
                return null;
            }

            string parsedValue = qrData.Trim();

            // Se é uma URL, extrair o ID da peça
            const string urlPrefix = "https://retro.tech/piece/";
            if (parsedValue.StartsWith(urlPrefix, System.StringComparison.OrdinalIgnoreCase))
            {
                parsedValue = parsedValue.Substring(urlPrefix.Length);
            }

            if (long.TryParse(parsedValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out long pieceId))
            {
                foreach (var piece in SampleData.Pieces)
                {
                    if (piece.Id == pieceId)
                    {
                        return piece;
                    }
                }
            }

            // Se não encontrou por ID, tentar por nome
            foreach (var piece in SampleData.Pieces)
            {
                if (piece.Name.Equals(parsedValue, System.StringComparison.OrdinalIgnoreCase))
                {
                    return piece;
                }
            }

            return null;
        }

        /// <summary>
        /// Cria um cartão com efeito glass - versão corrigida
        /// </summary>
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
            cardLE.flexibleHeight = 0f;

            var cardImg = cardGO.GetComponent<Image>();
            cardImg.color = new Color(1f, 1f, 1f, 0.14f); // Glass effect
            cardImg.raycastTarget = true;

            // Add rounded corners if available
            var roundedSprite = Resources.Load<Sprite>("Sprites/RoundedPanel");
            if (roundedSprite != null)
            {
                cardImg.sprite = roundedSprite;
                cardImg.type = Image.Type.Sliced;
            }

            // Subtle outline and drop shadow for better visual hierarchy
            var outline = cardGO.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.18f);
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;

            var shadow = cardGO.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
            shadow.effectDistance = new Vector2(0f, -6f);
            shadow.useGraphicAlpha = false;

            return cardImg;
        }

        /// <summary>
        /// Cria um botão CTA (Call To Action) - versão corrigida
        /// </summary>
        private GameObject CreatePrototypeCTAButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            float buttonHeight = EvaluateResponsiveHeight(48f, 0.12f, 0.18f, 0.9f, 1.3f);
            var btnCard = CreateGlassCard(parent, buttonHeight);
            btnCard.color = Color.white; // Botão branco como no protótipo

            var btn = btnCard.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnCard;
            if (onClick != null)
                btn.onClick.AddListener(onClick);

            var colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            btn.colors = colors;

            var label = UiKit.TMP(btnCard.transform, text, 18,
                new Color32(103, 80, 164, 255), TextAlignmentOptions.Center, bold: true);
            label.enableWordWrapping = false;
            label.raycastTarget = false;

            var labelRT = label.rectTransform;
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            float horizontalInset = ResponsiveTypography.ResponsiveSpacing(16f);
            float verticalInset = ResponsiveTypography.ResponsiveSpacing(8f);
            labelRT.offsetMin = new Vector2(horizontalInset, verticalInset);
            labelRT.offsetMax = new Vector2(-horizontalInset, -verticalInset);

            return btnCard.gameObject;
        }

        /// <summary>
        /// Adiciona espaço vertical
        /// </summary>
        private void AddSpacer(Transform parent, float height)
        {
            var spacerGO = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacerGO.transform.SetParent(parent, false);
            var le = spacerGO.GetComponent<LayoutElement>();
            float responsiveHeight = ResponsiveTypography.ResponsiveSpacing(height);
            le.minHeight = responsiveHeight;
            le.preferredHeight = responsiveHeight;
            le.flexibleHeight = 0f;
        }

        /// <summary>
        /// Define se a página está ativa ou não
        /// </summary>
        public void SetActive(bool active)
        {
            if (_pageObject != null)
            {
                _pageObject.SetActive(active);

                // Parar scan quando a página ficar inativa
                if (!active && _isScanning)
                {
                    StopScan();
                }
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
        /// Atualiza as configurações visuais da página
        /// </summary>
        public void UpdateVisualSettings(float previewHeight, float tipsHeight, float frameSize)
        {
            previewCardHeight = previewHeight;
            tipsCardHeight = tipsHeight;
            qrFrameSize = frameSize;
            ApplyResponsiveMetrics();
        }

        /// <summary>
        /// Atualiza os tamanhos das fontes
        /// </summary>
        public void UpdateFontSizes(int titleSize, int scanTitleSize, int descSize, int tipsSize)
        {
            titleFontSize = titleSize;
            scanTitleFontSize = scanTitleSize;
            scanDescFontSize = descSize;
            tipsFontSize = tipsSize;
            ApplyResponsiveMetrics();
        }

        /// <summary>
        /// Verifica se está escaneando atualmente
        /// </summary>
        public bool IsScanning()
        {
            return _isScanning;
        }

        /// <summary>
        /// Limpa recursos da página
        /// </summary>
        private void OnDestroy()
        {
            StopScan();
            OnPieceScanned = null;

#if ZXING_PRESENT
            _qrReader = null;
#endif
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_pageInitialized)
            {
                ApplyResponsiveMetrics();
            }
        }

        #region Editor Methods
#if UNITY_EDITOR
        /// <summary>
        /// Valida as configurações no editor
        /// </summary>
        private void OnValidate()
        {
            // Garantir que os valores sejam válidos
            previewCardHeight = Mathf.Max(200f, previewCardHeight);
            tipsCardHeight = Mathf.Max(100f, tipsCardHeight);
            qrFrameSize = Mathf.Max(100f, qrFrameSize);
            titleFontSize = Mathf.Max(12, titleFontSize);
            scanTitleFontSize = Mathf.Max(12, scanTitleFontSize);
            scanDescFontSize = Mathf.Max(8, scanDescFontSize);
            tipsTitleFontSize = Mathf.Max(8, tipsTitleFontSize);
            tipsFontSize = Mathf.Max(8, tipsFontSize);
        }
#endif
        #endregion

        private float GetCanvasScaleFactor()
        {
            if (_parentCanvas == null)
                return 1f;

            return Mathf.Max(0.1f, _parentCanvas.scaleFactor);
        }

        private float GetCanvasHeight()
        {
            if (_parentCanvas == null)
                return -1f;

            return _parentCanvas.pixelRect.height / GetCanvasScaleFactor();
        }

        private float GetCanvasWidth()
        {
            if (_parentCanvas == null)
                return -1f;

            return _parentCanvas.pixelRect.width / GetCanvasScaleFactor();
        }

        private float GetCanvasMinDimension()
        {
            float width = GetCanvasWidth();
            float height = GetCanvasHeight();

            if (width <= 0f || height <= 0f)
                return -1f;

            return Mathf.Min(width, height);
        }

        private bool IsLandscapeLayout()
        {
            return Screen.width > Screen.height;
        }

        private float EvaluateResponsiveHeight(float baseHeight, float portraitRatio, float landscapeRatio, float minMultiplier, float maxMultiplier)
        {
            float height = baseHeight;
            float canvasHeight = GetCanvasHeight();

            if (canvasHeight > 0f)
            {
                float ratio = IsLandscapeLayout() ? landscapeRatio : portraitRatio;
                height = Mathf.Clamp(canvasHeight * ratio, baseHeight * minMultiplier, baseHeight * maxMultiplier);
            }

            return ResponsiveTypography.ResponsiveSpacing(height);
        }

        private float EvaluateResponsiveSquareSize(float baseSize, float portraitRatio, float landscapeRatio, float minMultiplier, float maxMultiplier)
        {
            float size = baseSize;
            float minDimension = GetCanvasMinDimension();

            if (minDimension > 0f)
            {
                float ratio = IsLandscapeLayout() ? landscapeRatio : portraitRatio;
                size = Mathf.Clamp(minDimension * ratio, baseSize * minMultiplier, baseSize * maxMultiplier);
            }

            return ResponsiveTypography.ResponsiveSpacing(size);
        }

        private float GetResponsivePreviewHeight()
        {
            return EvaluateResponsiveHeight(previewCardHeight, 0.42f, 0.55f, 0.75f, 1.55f);
        }

        private float GetResponsiveTipsHeight()
        {
            return EvaluateResponsiveHeight(tipsCardHeight, 0.22f, 0.32f, 0.8f, 1.4f);
        }

        private float GetResponsiveFrameSize()
        {
            return EvaluateResponsiveSquareSize(qrFrameSize, 0.28f, 0.32f, 0.7f, 1.4f);
        }

        private void ApplyResponsiveMetrics()
        {
            if (!_pageInitialized)
                return;

            float responsivePreview = GetResponsivePreviewHeight();
            if (_previewCardLayout != null)
            {
                _previewCardLayout.preferredHeight = responsivePreview;
                _previewCardLayout.minHeight = responsivePreview;
            }

            float responsiveTips = GetResponsiveTipsHeight();
            if (_tipsCardLayout != null)
            {
                _tipsCardLayout.preferredHeight = responsiveTips;
                _tipsCardLayout.minHeight = responsiveTips;
            }

            float responsiveFrame = GetResponsiveFrameSize();
            if (_qrFrameLayout != null)
            {
                _qrFrameLayout.preferredWidth = responsiveFrame;
                _qrFrameLayout.preferredHeight = responsiveFrame;
                _qrFrameLayout.minWidth = responsiveFrame;
                _qrFrameLayout.minHeight = responsiveFrame;
            }

            if (_qrFrameRect != null)
            {
                _qrFrameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, responsiveFrame);
                _qrFrameRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, responsiveFrame);
            }

            if (_contentContainer != null)
            {
                LayoutRebuilder.MarkLayoutForRebuild(_contentContainer);
            }
        }
    }
}
