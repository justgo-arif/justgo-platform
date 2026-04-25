namespace AuthModule.Application.DTOs.Notes
{
    public class NoteDTO
    {
        public Guid NotesGuid { get; set; }
        public string Details { get; set; }
        public string UserName { get; set; }
        public string ProfilePicURL { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
