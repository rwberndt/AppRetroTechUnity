using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static RetroTech.UiKit;

namespace RetroTech
{
    /// <summary>
    /// Página do quiz do RetroTech que permite aos usuários testarem seus conhecimentos
    /// sobre peças de computação retro através de perguntas interativas.
    /// Inclui sistema de pontuação, feedback visual e salvamento de high score.
    /// </summary>
    public class QuizPage : MonoBehaviour
    {
        [Header("Page Configuration")]
        [SerializeField] private string pageTitle = "QuizPage";

        [Header("Visual Configuration")]
        [SerializeField] private float headerHeight = 60f;
        [SerializeField] private float questionCardHeight = 120f;
        [SerializeField] private float optionHeight = 48f;
        [SerializeField] private float explanationHeight = 100f;
        [SerializeField] private int titleFontSize = 36;
        [SerializeField] private int scoreFontSize = 26;
        [SerializeField] private int questionFontSize = 28;
        [SerializeField] private int optionFontSize = 22;

        // Colors
        private readonly Color HeaderColor = new Color32(255, 255, 255, 15);
        private readonly Color QuestionColor = new Color32(255, 255, 255, 10);
        private readonly Color OptionColor = Color.white;
        private readonly Color OptionTextColor = new Color32(103, 80, 164, 255);
        private readonly Color ProgressBackgroundColor = new Color32(255, 255, 255, 51); // 20% opacity
        private readonly Color ProgressFillColor = Color.white;
        private readonly Color CorrectOptionColor = new Color32(76, 175, 80, 255); // Green
        private readonly Color WrongOptionColor = new Color32(244, 67, 54, 255); // Red
        private readonly Color DisabledOptionColor = new Color32(200, 200, 200, 255); // Gray

        // Events
        public System.Action<int, int> OnScoreUpdated; // currentScore, totalQuestions
        public System.Action<int> OnQuizCompleted; // finalScore
        public System.Action<QuizQuestion, bool> OnQuestionAnswered; // question, wasCorrect

        // State
        private int _currentQuizIndex;
        private int _quizScore;
        private GameObject _pageObject;
        private RectTransform _contentContainer;

        // UI References
        private TextMeshProUGUI _scoreLabel;
        private Image _progressFill;
        private GameObject _nextButton;
        private TextMeshProUGUI _explanationText;
        private TextMeshProUGUI _questionText;
        private GameObject _optionsContainer;
        private GameObject _explanationCard;

        /// <summary>
        /// Cria e configura a página do quiz
        /// </summary>
        /// <param name="parent">Transform pai onde a página será criada</param>
        /// <param name="buildSurfaceFunc">Função para criar a superfície base da página</param>
        /// <param name="createGlassCardFunc">Função para criar cards com efeito glass</param>
        /// <param name="createCTAButtonFunc">Função para criar botões CTA</param>
        /// <returns>GameObject da página criada</returns>
        public GameObject CreatePage(
            Transform parent,
            System.Func<string, (GameObject surface, RectTransform content)> buildSurfaceFunc,
            System.Func<Transform, float, Image> createGlassCardFunc,
            System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> createCTAButtonFunc)
        {
            var (surface, content) = buildSurfaceFunc(pageTitle);
            _pageObject = surface.gameObject;
            _contentContainer = content;

            CreateQuizContent(createGlassCardFunc, createCTAButtonFunc);
            InitializeQuiz();

            return _pageObject;
        }

        /// <summary>
        /// Cria todo o conteúdo do quiz
        /// </summary>
        private void CreateQuizContent(
            System.Func<Transform, float, Image> createGlassCardFunc,
            System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> createCTAButtonFunc)
        {
            // Quiz header with progress
            CreateQuizHeader(createGlassCardFunc);

            // Progress bar
            CreateProgressBar(createGlassCardFunc);

            // Question card
            CreateQuestionCard(createGlassCardFunc);

            // Options container
            CreateOptionsContainer();

            // Explanation card
            CreateExplanationCard(createGlassCardFunc);

            // Next button
            CreateNextButton(createCTAButtonFunc);
        }

        /// <summary>
        /// Cria o cabeçalho do quiz com título e pontuação
        /// </summary>
        private void CreateQuizHeader(System.Func<Transform, float, Image> createGlassCardFunc)
        {
            var headerCard = createGlassCardFunc(_contentContainer, headerHeight);
            headerCard.color = HeaderColor;

            var headerHLG = headerCard.gameObject.AddComponent<HorizontalLayoutGroup>();
            headerHLG.padding = new RectOffset(20, 20, 12, 12);
            headerHLG.spacing = 16;
            headerHLG.childAlignment = TextAnchor.MiddleCenter;
            headerHLG.childForceExpandWidth = false;

            // Title
            var quizTitleTMP = UiKit.TMP(headerCard.transform, "Quiz RetroTech", titleFontSize,
                Color.white, TextAlignmentOptions.Left, bold: true);
            quizTitleTMP.enableWordWrapping = false;

            // Spacer
            var spacerGO = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacerGO.transform.SetParent(headerCard.transform, false);
            spacerGO.GetComponent<LayoutElement>().flexibleWidth = 1;

            // Score label
            _scoreLabel = (TextMeshProUGUI)UiKit.TMP(headerCard.transform, "Pontuação: 0", scoreFontSize,
                Color.white, TextAlignmentOptions.Right);
            _scoreLabel.enableWordWrapping = false;
        }

        /// <summary>
        /// Cria a barra de progresso do quiz
        /// </summary>
        private void CreateProgressBar(System.Func<Transform, float, Image> createGlassCardFunc)
        {
            var progressCard = createGlassCardFunc(_contentContainer, 12f);
            progressCard.color = ProgressBackgroundColor;

            var fillGO = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(progressCard.transform, false);

            var fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = new Vector2(0, 1);
            fillRT.offsetMin = new Vector2(2, 2);
            fillRT.offsetMax = new Vector2(2, -2);

            _progressFill = fillGO.GetComponent<Image>();
            _progressFill.color = ProgressFillColor;
            _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal;
            _progressFill.fillAmount = 0f;
        }

        /// <summary>
        /// Cria o card da pergunta
        /// </summary>
        private void CreateQuestionCard(System.Func<Transform, float, Image> createGlassCardFunc)
        {
            var questionCard = createGlassCardFunc(_contentContainer, questionCardHeight);
            questionCard.color = QuestionColor;

            var questionVLG = questionCard.gameObject.AddComponent<VerticalLayoutGroup>();
            questionVLG.padding = new RectOffset(20, 20, 16, 16);
            questionVLG.spacing = 0;

            _questionText = (TextMeshProUGUI)UiKit.TMP(questionCard.transform, "", questionFontSize,
                Color.white, TextAlignmentOptions.TopLeft);
            _questionText.enableWordWrapping = true;
        }

        /// <summary>
        /// Cria o container das opções de resposta
        /// </summary>
        private void CreateOptionsContainer()
        {
            _optionsContainer = new GameObject("Options", typeof(RectTransform), typeof(VerticalLayoutGroup));
            _optionsContainer.transform.SetParent(_contentContainer, false);

            var optionsVLG = _optionsContainer.GetComponent<VerticalLayoutGroup>();
            optionsVLG.spacing = 12;
            optionsVLG.padding = new RectOffset(0, 0, 8, 8);
            optionsVLG.childControlHeight = true;
            optionsVLG.childForceExpandHeight = false;
            optionsVLG.childControlWidth = true;
            optionsVLG.childForceExpandWidth = true;
        }

        /// <summary>
        /// Cria o card de explicação da resposta
        /// </summary>
        private void CreateExplanationCard(System.Func<Transform, float, Image> createGlassCardFunc)
        {
            var expCard = createGlassCardFunc(_contentContainer, explanationHeight);
            _explanationCard = expCard.gameObject;

            var expVLG = expCard.gameObject.AddComponent<VerticalLayoutGroup>();
            expVLG.padding = new RectOffset(16, 16, 12, 12);
            expVLG.spacing = 8;

            var expTitle = UiKit.TMP(expCard.transform, "❌ Incorreto!", 22, Color.white,
                TextAlignmentOptions.Left, bold: true);
            expTitle.name = "ExpTitle";

            _explanationText = (TextMeshProUGUI)UiKit.TMP(expCard.transform, "", 22,
                new Color32(255, 255, 255, 200), TextAlignmentOptions.Left);
            _explanationText.enableWordWrapping = true;

            _explanationCard.SetActive(false);
        }

        /// <summary>
        /// Cria o botão "Próxima Pergunta"
        /// </summary>
        private void CreateNextButton(System.Func<Transform, string, UnityEngine.Events.UnityAction, GameObject> createCTAButtonFunc)
        {
            _nextButton = createCTAButtonFunc(_contentContainer.transform, "Próxima Pergunta", OnNextButtonClicked);
            _nextButton.SetActive(false);
        }

        /// <summary>
        /// Inicializa o quiz
        /// </summary>
        private void InitializeQuiz()
        {
            _currentQuizIndex = 0;
            _quizScore = 0;
            UpdateUI();
        }

        /// <summary>
        /// Atualiza a interface do usuário com a pergunta atual
        /// </summary>
        private void UpdateUI()
        {
            if (_currentQuizIndex >= SampleData.QuizQuestions.Count)
            {
                ShowQuizResult();
                return;
            }

            // Clear previous options
            foreach (Transform child in _optionsContainer.transform)
                Destroy(child.gameObject);

            _explanationCard.SetActive(false);
            _nextButton.SetActive(false);

            var question = SampleData.QuizQuestions[_currentQuizIndex];

            // Update score and progress
            _scoreLabel.text = $"Pontuação: {_quizScore}";
            _progressFill.fillAmount = (_currentQuizIndex) / Mathf.Max(1f, (float)(SampleData.QuizQuestions.Count - 1));

            // Update question
            _questionText.text = question.Question;

            // Create option buttons
            CreateOptionButtons(question);

            OnScoreUpdated?.Invoke(_quizScore, SampleData.QuizQuestions.Count);
        }

        /// <summary>
        /// Cria os botões das opções de resposta
        /// </summary>
        private void CreateOptionButtons(QuizQuestion question)
        {
            for (int i = 0; i < question.Options.Count; i++)
            {
                int optionIndex = i;
                CreateOptionButton(question.Options[i], optionIndex, question);
            }
        }

        /// <summary>
        /// Cria um botão de opção individual
        /// </summary>
        private void CreateOptionButton(string optionText, int optionIndex, QuizQuestion question)
        {
            var optionCard = UiKit.CreateCard(_optionsContainer.transform, new Vector2(0, optionHeight),
                OptionColor, 16f, glass: false);

            var btn = optionCard.gameObject.AddComponent<Button>();
            btn.targetGraphic = optionCard;

            var optionTMP = UiKit.TMP(optionCard.transform, optionText, optionFontSize,
                OptionTextColor, TextAlignmentOptions.Center, bold: true);
            optionTMP.enableWordWrapping = false;
            optionTMP.raycastTarget = false;

            var optionRT = optionTMP.rectTransform;
            optionRT.anchorMin = Vector2.zero;
            optionRT.anchorMax = Vector2.one;
            optionRT.offsetMin = new Vector2(16, 8);
            optionRT.offsetMax = new Vector2(-16, -8);

            btn.onClick.AddListener(() => OnOptionSelected(optionIndex, question));
        }

        /// <summary>
        /// Manipula a seleção de uma opção
        /// </summary>
        private void OnOptionSelected(int selectedIndex, QuizQuestion question)
        {
            bool isCorrect = selectedIndex == question.CorrectAnswerIndex;

            if (isCorrect)
                _quizScore++;

            // Update explanation
            var expTitle = _explanationCard.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            expTitle.text = isCorrect ? "✅ Correto!" : "❌ Incorreto!";
            _explanationText.text = question.Explanation;
            _explanationCard.SetActive(true);

            // Update option colors
            UpdateOptionColors(selectedIndex, question.CorrectAnswerIndex);

            // Update score
            _scoreLabel.text = $"Pontuação: {_quizScore}";
            _nextButton.SetActive(true);

            // Trigger events
            OnQuestionAnswered?.Invoke(question, isCorrect);
            OnScoreUpdated?.Invoke(_quizScore, SampleData.QuizQuestions.Count);
        }

        /// <summary>
        /// Atualiza as cores das opções após a seleção
        /// </summary>
        private void UpdateOptionColors(int selectedIndex, int correctIndex)
        {
            for (int i = 0; i < _optionsContainer.transform.childCount; i++)
            {
                var option = _optionsContainer.transform.GetChild(i);
                var img = option.GetComponent<Image>();
                var txt = option.GetComponentInChildren<TextMeshProUGUI>();
                var button = option.GetComponent<Button>();

                if (button)
                    button.interactable = false;

                if (i == correctIndex)
                {
                    img.color = CorrectOptionColor;
                    txt.color = Color.white;
                }
                else if (i == selectedIndex)
                {
                    img.color = WrongOptionColor;
                    txt.color = Color.white;
                }
                else
                {
                    img.color = DisabledOptionColor;
                    txt.color = new Color32(100, 100, 100, 255);
                }
            }
        }

        /// <summary>
        /// Manipula o clique no botão "Próxima Pergunta"
        /// </summary>
        private void OnNextButtonClicked()
        {
            _currentQuizIndex++;
            UpdateUI();
        }

        /// <summary>
        /// Mostra o resultado final do quiz
        /// </summary>
        private void ShowQuizResult()
        {
            // Clear all content
            foreach (Transform child in _contentContainer)
                Destroy(child.gameObject);

            var resultCard = UiKit.CreateCard(_contentContainer, new Vector2(0, 200f),
                new Color(1f, 1f, 1f, 0.1f), 16f, glass: true);

            var resultVLG = resultCard.gameObject.AddComponent<VerticalLayoutGroup>();
            resultVLG.padding = new RectOffset(24, 24, 24, 24);
            resultVLG.spacing = 16;
            resultVLG.childAlignment = TextAnchor.MiddleCenter;

            // Score text
            var scoreTMP = UiKit.TMP(resultCard.transform,
                $"Você acertou {_quizScore} de {SampleData.QuizQuestions.Count} perguntas!",
                32, Color.white, TextAlignmentOptions.Center, bold: true);

            // Message based on performance
            string message = GetResultMessage();
            var msgTMP = UiKit.TMP(resultCard.transform, message, 24,
                new Color32(255, 255, 255, 200), TextAlignmentOptions.Center);

            // Save high score
            SaveHighScore();

            // Restart button
            var restartCard = UiKit.CreateCard(resultCard.transform, new Vector2(0, 48f),
                Color.white, 16f, glass: false);

            var restartBtn = restartCard.gameObject.AddComponent<Button>();
            restartBtn.targetGraphic = restartCard;
            restartBtn.onClick.AddListener(RestartQuiz);

            var restartTMP = UiKit.TMP(restartCard.transform, "Reiniciar Quiz", 24,
                OptionTextColor, TextAlignmentOptions.Center, bold: true);
            restartTMP.raycastTarget = false;

            var restartRT = restartTMP.rectTransform;
            restartRT.anchorMin = Vector2.zero;
            restartRT.anchorMax = Vector2.one;
            restartRT.offsetMin = new Vector2(16, 8);
            restartRT.offsetMax = new Vector2(-16, -8);

            OnQuizCompleted?.Invoke(_quizScore);
        }

        /// <summary>
        /// Obtém a mensagem de resultado baseada na performance
        /// </summary>
        private string GetResultMessage()
        {
            if (_quizScore == SampleData.QuizQuestions.Count)
                return "Excelente! Você é um expert em tecnologia retrô.";
            else if (_quizScore >= SampleData.QuizQuestions.Count / 2)
                return "Muito bom! Continue explorando para aprender mais.";
            else
                return "Você pode melhorar! Que tal estudar mais sobre as peças?";
        }

        /// <summary>
        /// Salva a pontuação máxima
        /// </summary>
        private void SaveHighScore()
        {
            int previousHigh = PlayerPrefs.GetInt("RetroTech_HighScore", 0);
            if (_quizScore > previousHigh)
            {
                PlayerPrefs.SetInt("RetroTech_HighScore", _quizScore);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Reinicia o quiz
        /// </summary>
        public void RestartQuiz()
        {
            _currentQuizIndex = 0;
            _quizScore = 0;

            // Clear result content
            foreach (Transform child in _contentContainer)
                Destroy(child.gameObject);

            // Recreate quiz content
            // Note: This would need the creation functions passed in again
            // For now, we'll just trigger initialization
            InitializeQuiz();
        }

        /// <summary>
        /// Define se a página está ativa ou não
        /// </summary>
        public void SetActive(bool active)
        {
            if (_pageObject != null)
                _pageObject.SetActive(active);
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
        public void UpdateVisualSettings(float headerH, float questionH, float optionH, float explanationH,
                                       int titleSize, int scoreSize, int questionSize, int optionSize)
        {
            headerHeight = headerH;
            questionCardHeight = questionH;
            optionHeight = optionH;
            explanationHeight = explanationH;
            titleFontSize = titleSize;
            scoreFontSize = scoreSize;
            questionFontSize = questionSize;
            optionFontSize = optionSize;
        }

        /// <summary>
        /// Obtém a pontuação atual do quiz
        /// </summary>
        public int GetCurrentScore()
        {
            return _quizScore;
        }

        /// <summary>
        /// Obtém o índice da pergunta atual
        /// </summary>
        public int GetCurrentQuestionIndex()
        {
            return _currentQuizIndex;
        }

        /// <summary>
        /// Obtém a pontuação máxima salva
        /// </summary>
        public int GetHighScore()
        {
            return PlayerPrefs.GetInt("RetroTech_HighScore", 0);
        }

        /// <summary>
        /// Limpa recursos da página
        /// </summary>
        private void OnDestroy()
        {
            OnScoreUpdated = null;
            OnQuizCompleted = null;
            OnQuestionAnswered = null;
        }

        #region Editor Methods
#if UNITY_EDITOR
        /// <summary>
        /// Valida as configurações no editor
        /// </summary>
        private void OnValidate()
        {
            headerHeight = Mathf.Max(40f, headerHeight);
            questionCardHeight = Mathf.Max(60f, questionCardHeight);
            optionHeight = Mathf.Max(30f, optionHeight);
            explanationHeight = Mathf.Max(60f, explanationHeight);
            titleFontSize = Mathf.Max(8, titleFontSize);
            scoreFontSize = Mathf.Max(8, scoreFontSize);
            questionFontSize = Mathf.Max(8, questionFontSize);
            optionFontSize = Mathf.Max(8, optionFontSize);
        }
#endif
        #endregion
    }
}