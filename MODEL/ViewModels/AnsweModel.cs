namespace MODEL.ViewModels
{
    public class AnswerModel
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public int VoteCount { get; set; }
        public string Content { get; set; }
    }
}
