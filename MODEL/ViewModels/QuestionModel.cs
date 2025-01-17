namespace MODEL.ViewModels
{
    public class QuestionModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Language { get; set; }
        public int TolalVoteCount { get; set; }
        public List<AnswerModel> AnswerList { get; set; }
    }
}