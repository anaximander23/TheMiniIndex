using MediatR;
using MiniIndex.Models;
using System.Collections.Generic;

namespace MiniIndex.Core.Tags
{
    public class InferTagsRequest : IRequest<IEnumerable<Tag>>
    {
        public InferTagsRequest(int miniId)
        {
            MiniId = miniId;
        }

        public int MiniId { get; }
    }
}