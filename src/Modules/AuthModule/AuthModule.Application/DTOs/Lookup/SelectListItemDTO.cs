namespace AuthModule.Application.DTOs.Lookup
{
    public class SelectListItemDTO<T>
    {
        public T Id { get; set; }       
        public string Text { get; set; } 
        public string Value { get; set; } 
    }
}


