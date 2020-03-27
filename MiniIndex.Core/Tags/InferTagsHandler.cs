using Anaximander.Linq;
using MediatR;
using MiniIndex.Models;
using MiniIndex.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiniIndex.Core.Tags
{
    public class InferTagsHandler : IRequestHandler<InferTagsRequest, IEnumerable<Tag>>
    {
        public InferTagsHandler(MiniIndexContext context)
        {
            _context = context;
        }

        private readonly MiniIndexContext _context;

        public Task<IEnumerable<Tag>> Handle(InferTagsRequest request, CancellationToken cancellationToken)
        {
            var mini = _context.Mini.SingleOrDefault(m => m.ID == request.MiniId);

            if (mini is null)
            {
                return Task.FromResult(Enumerable.Empty<Tag>());
            }

            var sourceText = new[] { mini.Name };

            var allTokens = sourceText
                .Select(x => x.ToLowerInvariant())
                .SelectMany(x => x.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries));

            var tokenCounts = allTokens
                .GroupBy(x => x)
                .Select(x => (term: x.Key, count: x.Count()))
                .ToList();

            var possibleTags = tokenCounts
                .SelectMany(term => _context.Tag
                    .Where(tag => tag.TagName.ToLower() == term.term))
                .Distinct()
                .Select(tag => new
                {
                    term = tag,
                    count = tag.MiniTags.Count()
                })
                .ToList();

            var twoWordTags = allTokens
                .Window(2)
                .Select(pair => String.Join(' ', pair).ToLowerInvariant())
                .GroupBy(x => x)
                .Select(x => (term: x.Key, count: x.Count()))
                .SelectMany(term => _context.Tag
                    .Where(tag => tag.TagName.ToLower() == term.term))
                .Distinct()
                .Select(tag => new
                {
                    term = tag,
                    count = tag.MiniTags.Count()
                })
                .ToList();

            var selectedTags = twoWordTags.OrderBy(x => x.count).Take(2)
                .Concat(twoWordTags.OrderByDescending(x => x.count).Take(2))
                .Concat(possibleTags.OrderBy(x => x.count).Take(2))
                .Concat(possibleTags.OrderByDescending(x => x.count).Take(2))
                .Select(x => x.term)
                .Distinct();

            return Task.FromResult(selectedTags.ToList().AsEnumerable());
        }
    }
}