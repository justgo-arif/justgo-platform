using JustGo.Result.Application.Common.Enums;

namespace JustGo.Result.Application.DTOs.ImportExportFileDtos
{
    public class ConfirmMemberFileDto
    {
        public int FileId { get; set; }
        public required string WebSocketId { get; set; }
        public List<ConfirmMemberDataDto> ConfirmMemberHeaders { get; set; } = [];
        public ICollection<ConfirmMemberDataDto> ConfirmedSecondSheetHeaders { get; set; } = [];

        public string GetMemberIdentifierColumn()
        {
            var identifierColumn = ConfirmMemberHeaders.FirstOrDefault(c => c is
                { IsMapped: true, ColumnIdentifier: (int)ResultUploadColumn.MemberId });
            return identifierColumn != null ? identifierColumn.SystemColumnName : string.Empty;
        }
    }

    public class ConfirmMemberDataDto
    {
        public string SystemColumnName { get; set; } = string.Empty;
        public string FileHeaderName { get; set; } = string.Empty;
        public bool IsMapped { get; set; }
        public required int ColumnIdentifier { get; set; }
    }
}