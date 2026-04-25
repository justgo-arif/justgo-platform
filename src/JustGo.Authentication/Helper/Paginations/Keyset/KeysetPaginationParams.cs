namespace JustGo.Authentication.Helper.Paginations.Keyset
{
    public class KeysetPaginationParams
    {
        public int? LastSeenId { get; set; }
        public int PageSize { get; set; } = 20;
    }
}
