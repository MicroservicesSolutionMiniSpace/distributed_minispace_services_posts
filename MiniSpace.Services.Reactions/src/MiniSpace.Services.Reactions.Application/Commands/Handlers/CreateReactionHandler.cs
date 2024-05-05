using Convey.CQRS.Commands;
using MiniSpace.Services.Reactions.Application.Events;
using MiniSpace.Services.Reactions.Application.Exceptions;
using MiniSpace.Services.Reactions.Application.Services;
using MiniSpace.Services.Reactions.Core.Entities;
using MiniSpace.Services.Reactions.Core.Exceptions;
using MiniSpace.Services.Reactions.Core.Repositories;

namespace MiniSpace.Services.Reactions.Application.Commands.Handlers
{
    public class CreateReactionHandler(IReactionRepository reactionRepository,
                                 IPostRepository postRepository,
                                 IEventRepository eventRepository,
                                 IDateTimeProvider dateTimeProvider,
                                 IAppContext appContext,
                                 IMessageBroker messageBroker
                                 ) : ICommandHandler<CreateReaction>
    {
        private readonly IReactionRepository _reactionRepository = reactionRepository;
        private readonly IPostRepository _postRepository = postRepository;
        private readonly IEventRepository _eventRepository = eventRepository;
        private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
        private readonly IMessageBroker _messageBroker = messageBroker;
        private readonly IAppContext _appContext = appContext;

        public async Task HandleAsync(CreateReaction command, CancellationToken cancellationToken = default)
        {
            var identity = _appContext.Identity;

            if (identity.Id != command.StudentId) {
                throw new UnauthorizedIdentityException(command.StudentId);
            }

            // Check the content type
            if (!Enum.TryParse<ReactionContentType>(command.ContentType, true, out var contentType)) {
                throw new InvalidReactionContentTypeException(command.ContentType);
            }

            // Check the content
            switch (contentType) {
                case ReactionContentType.Event:
                    if (!await _eventRepository.ExistsAsync(command.ContentId)) {
                        throw new EventNotFoundException(command.ContentId);
                    }
                    break;
                case ReactionContentType.Post:
                    if (!await _postRepository.ExistsAsync(command.ContentId)) {
                        throw new PostNotFoundException(command.ContentId);
                    }
                    break;
                default:
                    break;
            }

            // check the reaction type
            // case-sensitive
            if (!Enum.TryParse<ReactionType>(command.ReactionType, false, out var reactionType))
            {
                throw new InvalidReactionTypeException(command.ReactionType);
            }
            
            var reaction = Reaction.Create(command.StudentId, reactionType, contentType, command.ContentId);
            await _reactionRepository.AddAsync(reaction);
            
            await _messageBroker.PublishAsync(new ReactionCreated(command.ReactionId));
        }
    }
}
