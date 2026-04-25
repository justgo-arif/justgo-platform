using JustGo.Authentication.Services.Interfaces.CustomMediator;

namespace JustGo.FieldManagement.Application.Features.EntityData.Commands.CreateEntityData
{
    public class CreateEntityDataCommand : IRequest<int>
    {

        public int ExId { get; set; }
        public int DocId { get; set; }
        public Dictionary<string, object> Data { get; }

        public CreateEntityDataCommand(int exId, int docId, Dictionary<string, object> data)
        {
            ExId = exId;
            DocId = docId;
            Data = data;
        }
    }
}
