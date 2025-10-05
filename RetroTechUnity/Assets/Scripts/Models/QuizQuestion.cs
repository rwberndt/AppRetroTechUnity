using System.Collections.Generic;

namespace RetroTech
{
    /// <summary>
    /// Represents a single quiz question with possible answers, the index of the
    /// correct answer, an explanatory string and a reference to a related
    /// ComputerPiece.  The relatedPieceId can be used to provide more context
    /// after the user answers a question.
    /// </summary>
    [System.Serializable]
    public class QuizQuestion
    {
        public long Id;
        public string Question;
        public List<string> Options;
        public int CorrectAnswerIndex;
        public string Explanation;
        public long? RelatedPieceId;

        public QuizQuestion(
            long id,
            string question,
            List<string> options,
            int correctAnswerIndex,
            string explanation,
            long? relatedPieceId = null)
        {
            Id = id;
            Question = question;
            Options = options;
            CorrectAnswerIndex = correctAnswerIndex;
            Explanation = explanation;
            RelatedPieceId = relatedPieceId;
        }
    }
}