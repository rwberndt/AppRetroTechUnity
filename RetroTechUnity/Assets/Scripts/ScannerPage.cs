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
        [SerializeField] private float tipsCardHeight = 160f;
        [SerializeField] private float qrFrameSize = 180f;
        [SerializeField] private int titleFontSize = 36;
        [SerializeField] private int subtitleFontSize = 18;
        [SerializeField] private int scanTitleFontSize = 24;
        [SerializeField] private int scanDescFontSize = 18;
        [SerializeField] private int tipsTitleFontSize = 20;
        [SerializeField] private int tipsFontSize = 16;

        [Header("Color Configuration")]
        [SerializeField] private Color32 accentColor = new Color32(98, 0, 238, 255);
        [SerializeField] private Color32 accentSoftColor = new Color32(98, 0, 238, 48);
        [SerializeField] private Color32 readyStatusColor = new Color32(187, 134, 252, 255);
        [SerializeField] private Color32 scanningStatusColor = new Color32(3, 218, 197, 255);
        [SerializeField] private Color32 successStatusColor = new Color32(0, 200, 83, 255);
        [SerializeField] private Color32 warningStatusColor = new Color32(255, 138, 128, 255);

        // Events
        public System.Action<ComputerPiece> OnPieceScanned;

        // State
        private GameObject _pageObject;
        private RectTransform _contentContainer;
        private Canvas _parentCanvas;
        private Button _scanButton;
        private bool _isScanning = false;
        private TextMeshProUGUI _statusTMP;
        private TextMeshProUGUI _statusSubtitleTMP;
        private Image _statusBadgeImage;
        private Image _scanPulseImage;
        private Coroutine _pulseRoutine;
        private Color _statusBadgeBaseColor;
        private Color _pulseBaseColor;

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
        }

        /// <summary>
        /// Cria o título da página
        /// </summary>
        private void CreateTitle()
        {
            var titleContainer = new GameObject("TitleContainer", typeof(RectTransform));
            titleContainer.transform.SetParent(_contentContainer, false);

            var titleVLG = titleContainer.AddComponent<VerticalLayoutGroup>();
            titleVLG.childAlignment = TextAnchor.UpperLeft;
            titleVLG.childControlWidth = true;
            titleVLG.childForceExpandWidth = true;
            titleVLG.childControlHeight = false;
            titleVLG.childForceExpandHeight = false;
            titleVLG.spacing = 6f;

            var headingRow = new GameObject("HeadingRow", typeof(RectTransform));
            headingRow.transform.SetParent(titleContainer.transform, false);

            var headingHLG = headingRow.AddComponent<HorizontalLayoutGroup>();
            headingHLG.spacing = 12f;
            headingHLG.childAlignment = TextAnchor.MiddleLeft;
            headingHLG.childControlWidth = false;
            headingHLG.childForceExpandWidth = false;
            headingHLG.childControlHeight = false;
            headingHLG.childForceExpandHeight = false;

            var accentGO = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            accentGO.transform.SetParent(headingRow.transform, false);
            var accentRT = accentGO.GetComponent<RectTransform>();
            accentRT.sizeDelta = new Vector2(6f, 48f);
            var accentImg = accentGO.GetComponent<Image>();
            accentImg.color = accentColor;
            accentImg.raycastTarget = false;
            var accentLE = accentGO.AddComponent<LayoutElement>();
            accentLE.preferredWidth = 6f;
            accentLE.minWidth = 6f;
            accentLE.preferredHeight = 48f;
            accentLE.minHeight = 48f;

            var titleTMP = UiKit.TMP(headingRow.transform, "Scanner QR", titleFontSize,
                Color.white, TextAlignmentOptions.Left, bold: true);
            titleTMP.enableWordWrapping = false;

            var subtitleTMP = UiKit.TMP(titleContainer.transform,
                "Escaneie peças do museu e desbloqueie novas histórias.",
                subtitleFontSize, new Color32(255, 255, 255, 190), TextAlignmentOptions.Left);
            subtitleTMP.margin = new Vector4(0, 0, 0, 20f);
        }

        /// <summary>
        /// Cria a área de preview do QR Code
        /// </summary>
        private void CreatePreviewArea()
        {
            // Usar o mesmo padrão do GameManager para manter consistência
            var previewCard = CreateGlassCard(_contentContainer, previewCardHeight);

            var previewVLG = previewCard.gameObject.AddComponent<VerticalLayoutGroup>();
            previewVLG.childAlignment = TextAnchor.MiddleCenter;
            previewVLG.spacing = 16;
            previewVLG.padding = new RectOffset(24, 24, 24, 24);
            previewVLG.childControlWidth = true;
            previewVLG.childForceExpandWidth = true;
            previewVLG.childControlHeight = false;
            previewVLG.childForceExpandHeight = false;

            // QR Code frame
            CreateQRFrame(previewCard.transform);
            CreateStatusSection(previewCard.transform);
            SetStatus("Pronto para escanear", readyStatusColor,
                "Toque em \"Iniciar Scanner\" para começar");

            // Title
            var scanTitleTMP = UiKit.TMP(previewCard.transform, "Scanner QR Code",
                scanTitleFontSize, Color.white, TextAlignmentOptions.Center, bold: true);
            scanTitleTMP.enableWordWrapping = false;

            // Description
            var scanDescTMP = UiKit.TMP(previewCard.transform,
                "Aponte a câmera para o QR code de uma peça para ver seus detalhes.",
                scanDescFontSize, new Color32(255, 255, 255, 180), TextAlignmentOptions.Center);
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
            frameRT.sizeDelta = new Vector2(qrFrameSize, qrFrameSize);

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

            var pulseGO = new GameObject("ScanPulse", typeof(RectTransform), typeof(Image));
            pulseGO.transform.SetParent(frameGO.transform, false);
            var pulseRT = pulseGO.GetComponent<RectTransform>();
            pulseRT.anchorMin = Vector2.zero;
            pulseRT.anchorMax = Vector2.one;
            pulseRT.offsetMin = Vector2.zero;
            pulseRT.offsetMax = Vector2.zero;
            _scanPulseImage = pulseGO.GetComponent<Image>();
            _scanPulseImage.raycastTarget = false;
            _pulseBaseColor = (Color)accentSoftColor;
            _pulseBaseColor.a = accentSoftColor.a / 255f;
            _scanPulseImage.color = _pulseBaseColor;
            _scanPulseImage.enabled = false;

            var frameLE = frameGO.AddComponent<LayoutElement>();
            frameLE.preferredWidth = qrFrameSize;
            frameLE.preferredHeight = qrFrameSize;
            frameLE.minWidth = qrFrameSize;
            frameLE.minHeight = qrFrameSize;

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
            var tipsCard = CreateGlassCard(_contentContainer, tipsCardHeight);

            var tipsVLG = tipsCard.gameObject.AddComponent<VerticalLayoutGroup>();
            tipsVLG.childAlignment = TextAnchor.UpperLeft;
            tipsVLG.spacing = 8;
            tipsVLG.padding = new RectOffset(20, 20, 16, 16);
            tipsVLG.childControlWidth = true;
            tipsVLG.childForceExpandWidth = true;
            tipsVLG.childControlHeight = false;
            tipsVLG.childForceExpandHeight = false;

            var tipsTitleTMP = UiKit.TMP(tipsCard.transform, "Dicas para escanear:",
                tipsTitleFontSize, Color.white, bold: true);
            tipsTitleTMP.enableWordWrapping = false;

            CreateTipItem(tipsCard.transform, "🔆", "Mantenha o QR code bem iluminado e sem reflexos fortes.");
            CreateTipItem(tipsCard.transform, "🤳", "Segure o dispositivo com firmeza para facilitar a leitura.");
            CreateTipItem(tipsCard.transform, "🎯", "Posicione o código dentro da moldura para garantir o foco completo.");
        }

        /// <summary>
        /// Inicia o processo de escaneamento
        /// </summary>
        private void StartScan()
        {
            if (_isScanning) return;

            _isScanning = true;
            UpdateScanButtonText("Escaneando...");
            SetStatus("Escaneando...", scanningStatusColor,
                "Mantenha o QR code alinhado ao quadro luminoso.");
            StartScanPulse();

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
            if (_isScanning)
            {
                _isScanning = false;
                UpdateScanButtonText("⚡ Iniciar Scanner");
            }

            StopScanPulse();

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

            SetStatus("Pronto para escanear", readyStatusColor,
                "Toque em \"Iniciar Scanner\" para começar");
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

        /// <summary>
        /// Atualiza o texto e as cores do indicador de status do scanner
        /// </summary>
        private void SetStatus(string message, Color32 statusColor, string subtitle = null)
        {
            if (_statusTMP != null)
            {
                _statusTMP.text = message;
                _statusTMP.color = statusColor;
            }

            if (_statusSubtitleTMP != null)
            {
                bool hasSubtitle = !string.IsNullOrEmpty(subtitle);
                _statusSubtitleTMP.gameObject.SetActive(hasSubtitle);
                if (hasSubtitle)
                {
                    _statusSubtitleTMP.text = subtitle;
                    Color subtitleColor = statusColor;
                    subtitleColor.a = 0.75f;
                    _statusSubtitleTMP.color = subtitleColor;
                }
                else
                {
                    _statusSubtitleTMP.text = string.Empty;
                }
            }

            if (_statusBadgeImage != null)
            {
                _statusBadgeBaseColor = statusColor;
                _statusBadgeBaseColor.a = 0.24f;
                if (_pulseRoutine == null)
                {
                    _statusBadgeImage.color = _statusBadgeBaseColor;
                }
            }
        }

        /// <summary>
        /// Inicia a animação de pulso visual durante o scan
        /// </summary>
        private void StartScanPulse()
        {
            if (_scanPulseImage == null)
                return;

            _scanPulseImage.enabled = true;

            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
            }

            _pulseRoutine = StartCoroutine(PulseIndicator());
        }

        /// <summary>
        /// Encerra a animação de pulso e restaura as cores base
        /// </summary>
        private void StopScanPulse()
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
                _pulseRoutine = null;
            }

            if (_scanPulseImage != null)
            {
                _scanPulseImage.enabled = false;
            }

            ApplyStatusBaseVisuals();
        }

        /// <summary>
        /// Aplica as cores base do indicador de status e da moldura
        /// </summary>
        private void ApplyStatusBaseVisuals()
        {
            if (_scanPulseImage != null)
            {
                _scanPulseImage.color = _pulseBaseColor;
            }

            if (_statusBadgeImage != null)
            {
                if (_statusBadgeBaseColor.a <= 0f)
                {
                    _statusBadgeBaseColor.a = 0.24f;
                }

                _statusBadgeImage.color = _statusBadgeBaseColor;
            }
        }

        /// <summary>
        /// Corrotina que gera efeito de pulso enquanto o scanner está ativo
        /// </summary>
        private IEnumerator PulseIndicator()
        {
            float elapsed = 0f;

            while (_scanPulseImage != null && _scanPulseImage.enabled)
            {
                elapsed += Time.deltaTime * 2f;
                float pulse = (Mathf.Sin(elapsed) + 1f) * 0.5f;

                if (_scanPulseImage != null)
                {
                    var pulseColor = _pulseBaseColor;
                    pulseColor.a = Mathf.Lerp(0.05f, 0.2f, pulse);
                    _scanPulseImage.color = pulseColor;
                }

                if (_statusBadgeImage != null)
                {
                    var badgeColor = _statusBadgeBaseColor;
                    badgeColor.a = Mathf.Lerp(0.2f, 0.35f, pulse);
                    _statusBadgeImage.color = badgeColor;
                }

                yield return null;
            }

            ApplyStatusBaseVisuals();
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
                SetStatus("Nenhuma câmera encontrada", warningStatusColor,
                    "Usaremos um modo de demonstração com peças de exemplo.");
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
                SetStatus("Não foi possível iniciar a câmera", warningStatusColor,
                    "Verifique as permissões e tente novamente.");
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
            StopScanPulse();

            // Processar resultado
            if (foundPiece != null)
            {
                SetStatus($"Peça detectada: {foundPiece.Name}", successStatusColor,
                    "Toque para ver os detalhes completos.");
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
            StopScanPulse();

            // Selecionar peça aleatória para demonstração
            if (SampleData.Pieces.Count > 0)
            {
                int randomIndex = Random.Range(0, SampleData.Pieces.Count);
                var randomPiece = SampleData.Pieces[randomIndex];

                Debug.Log($"Simulando scan da peça: {randomPiece.Name}");
                SetStatus($"Peça encontrada: {randomPiece.Name}", successStatusColor,
                    "Toque para visualizar detalhes e curiosidades.");
                OnPieceScanned?.Invoke(randomPiece);
            }
            else
            {
                Debug.LogWarning("Nenhuma peça disponível para simulação");
                SetStatus("Nenhuma peça de demonstração", warningStatusColor,
                    "Adicione itens ao SampleData para testar o scanner.");
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
            cardImg.color = new Color(1f, 1f, 1f, 0.1f); // Glass effect
            cardImg.raycastTarget = true;

            // Add rounded corners if available
            var roundedSprite = Resources.Load<Sprite>("Sprites/RoundedPanel");
            if (roundedSprite != null)
            {
                cardImg.sprite = roundedSprite;
                cardImg.type = Image.Type.Sliced;
            }

            return cardImg;
        }

        /// <summary>
        /// Cria um botão CTA (Call To Action) - versão corrigida
        /// </summary>
        private GameObject CreatePrototypeCTAButton(Transform parent, string text, UnityEngine.Events.UnityAction onClick)
        {
            var btnCard = CreateGlassCard(parent, 48f);
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
            labelRT.offsetMin = new Vector2(16, 8);
            labelRT.offsetMax = new Vector2(-16, -8);

            return btnCard.gameObject;
        }

        /// <summary>
        /// Cria a seção de status do scanner com badge informativo
        /// </summary>
        private void CreateStatusSection(Transform parent)
        {
            var statusContainer = new GameObject("StatusSection", typeof(RectTransform));
            statusContainer.transform.SetParent(parent, false);

            var statusVLG = statusContainer.AddComponent<VerticalLayoutGroup>();
            statusVLG.childAlignment = TextAnchor.MiddleCenter;
            statusVLG.spacing = 6f;
            statusVLG.childControlWidth = true;
            statusVLG.childForceExpandWidth = false;
            statusVLG.childControlHeight = false;
            statusVLG.childForceExpandHeight = false;

            var badgeGO = new GameObject("StatusBadge", typeof(RectTransform), typeof(Image));
            badgeGO.transform.SetParent(statusContainer.transform, false);
            _statusBadgeImage = badgeGO.GetComponent<Image>();
            _statusBadgeBaseColor = new Color(1f, 1f, 1f, 0.18f);
            _statusBadgeImage.color = _statusBadgeBaseColor;
            _statusBadgeImage.raycastTarget = false;

            var badgeLayout = badgeGO.AddComponent<HorizontalLayoutGroup>();
            badgeLayout.childAlignment = TextAnchor.MiddleCenter;
            badgeLayout.childControlWidth = false;
            badgeLayout.childForceExpandWidth = false;
            badgeLayout.childControlHeight = false;
            badgeLayout.childForceExpandHeight = false;
            badgeLayout.padding = new RectOffset(18, 18, 10, 10);

            var badgeCSF = badgeGO.AddComponent<ContentSizeFitter>();
            badgeCSF.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            badgeCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _statusTMP = UiKit.TMP(badgeGO.transform, string.Empty, scanTitleFontSize,
                readyStatusColor, TextAlignmentOptions.Center, bold: true);
            _statusTMP.enableWordWrapping = false;

            var statusSubtitle = UiKit.TMP(statusContainer.transform, string.Empty, tipsFontSize,
                new Color32(255, 255, 255, 200), TextAlignmentOptions.Center);
            statusSubtitle.enableWordWrapping = true;
            statusSubtitle.margin = new Vector4(12, 0, 12, 0);
            statusSubtitle.gameObject.SetActive(false);
            _statusSubtitleTMP = statusSubtitle;
        }

        /// <summary>
        /// Cria um item individual dentro do cartão de dicas
        /// </summary>
        private void CreateTipItem(Transform parent, string icon, string text)
        {
            var row = new GameObject("TipRow", typeof(RectTransform));
            row.transform.SetParent(parent, false);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12f;
            hlg.childAlignment = TextAnchor.UpperLeft;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandHeight = false;

            var iconTMP = UiKit.TMP(row.transform, icon, tipsFontSize + 4,
                Color.white, TextAlignmentOptions.Center, bold: true);
            iconTMP.enableWordWrapping = false;

            var textTMP = UiKit.TMP(row.transform, text, tipsFontSize,
                new Color32(255, 255, 255, 200), TextAlignmentOptions.Left);
            textTMP.enableWordWrapping = true;

            var textLE = textTMP.gameObject.AddComponent<LayoutElement>();
            textLE.flexibleWidth = 1f;
            textLE.minWidth = 0f;
        }

        /// <summary>
        /// Adiciona espaço vertical
        /// </summary>
        private void AddSpacer(Transform parent, float height)
        {
            var spacerGO = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacerGO.transform.SetParent(parent, false);
            var le = spacerGO.GetComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;
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
    }
}