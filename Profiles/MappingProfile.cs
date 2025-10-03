using AutoMapper;
using Project_Version1.Data;
using Project_Version1.DTOs;
using System.Linq;
using System.Collections.Generic;

namespace Project_Version1.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<User, UserDto>()
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role ?? "Employee"))
                .ForMember(dest => dest.ProfilePicture, opt => opt.MapFrom(src => src.ProfilePicture));

            CreateMap<UserCreateDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            CreateMap<UserUpdateDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ProfilePicture, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

            CreateMap<RegisterDto, User>()
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // Post create -> entity
            CreateMap<PostCreateDto, Post>()
                .ForMember(dest => dest.PostId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpvoteCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.DownvoteCount, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.IsRepost, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.Attachments, opt => opt.Ignore())
                .ForMember(dest => dest.PostTags, opt => opt.Ignore());

            // Post -> PostBriefDto
            CreateMap<Post, PostBriefDto>()
                .ForMember(dest => dest.BodyPreview, opt => opt.MapFrom(src =>
                    string.IsNullOrEmpty(src.Body) ? string.Empty :
                    src.Body.Length > 200 ? src.Body.Substring(0, 200) + "..." : src.Body))
                .ForMember(dest => dest.DeptId, opt => opt.MapFrom(src => src.DeptId.ToString()))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId.ToString()))
                .ForMember(dest => dest.UpvoteCount, opt => opt.MapFrom(src => src.UpvoteCount ?? 0))
                .ForMember(dest => dest.DownvoteCount, opt => opt.MapFrom(src => src.DownvoteCount ?? 0))
                .ForMember(dest => dest.CommentsCount, opt => opt.MapFrom(src => src.Comments != null ? src.Comments.Count : 0))
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.FullName))
                // RepostedBy: use Select(...).FirstOrDefault() to avoid null-propagating operator
                .ForMember(dest => dest.RepostedBy, opt => opt.MapFrom(src =>
                    src.Reposts != null
                        ? src.Reposts.OrderByDescending(r => r.CreatedAt).Select(r => r.User.FullName).FirstOrDefault()
                        : null));

            // Post -> PostDetailDto
            CreateMap<Post, PostDetailDto>()
                .ForMember(dest => dest.Body, opt => opt.MapFrom(src => src.Body ?? string.Empty))
                .ForMember(dest => dest.UpvoteCount, opt => opt.MapFrom(src => src.UpvoteCount ?? 0))
                .ForMember(dest => dest.DownvoteCount, opt => opt.MapFrom(src => src.DownvoteCount ?? 0))
                .ForMember(dest => dest.CommentsCount, opt => opt.MapFrom(src => src.Comments != null ? src.Comments.Count : 0))
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Dept != null ? src.Dept.DeptName : null))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.PostTags != null ? src.PostTags.Select(pt => pt.Tag.TagName).ToList() : new List<string>()))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.DeptId, opt => opt.MapFrom(src => src.DeptId))
                .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments))
                .ForMember(dest => dest.RepostedBy, opt => opt.MapFrom(src =>
                    src.Reposts != null
                        ? src.Reposts.OrderByDescending(r => r.CreatedAt).Select(r => r.User.FullName).FirstOrDefault()
                        : null));

            CreateMap<Attachment, AttachmentDto>();

            // Tag -> TagDto (required by TagsController.ProjectTo<TagDto>)
            CreateMap<Tag, TagDto>();

            // Department -> DepartmentDto (fixes CategoriesController 500 error)
            CreateMap<Department, DepartmentDto>();

            // Comment mappings...
            CreateMap<CommentCreateDto, Comment>()
                .ForMember(dest => dest.CommentId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.UpvoteCount, opt => opt.Ignore())
                .ForMember(dest => dest.DownvoteCount, opt => opt.Ignore())
                .ForMember(dest => dest.UserVote, opt => opt.Ignore());

            CreateMap<Comment, CommentWithRepliesDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Replies, opt => opt.Ignore())
                .ForMember(dest => dest.UpvoteCount, opt => opt.Ignore())
                .ForMember(dest => dest.DownvoteCount, opt => opt.Ignore())
                .ForMember(dest => dest.UserVote, opt => opt.Ignore());

            CreateMap<Comment, CommentDetailDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.CommentText == "[Comment deleted]"))
                .ForMember(dest => dest.ReplyCount, opt => opt.MapFrom(src => src.InverseParentComment != null ? src.InverseParentComment.Count : 0))
                .ForMember(dest => dest.UpvoteCount, opt => opt.Ignore())
                .ForMember(dest => dest.DownvoteCount, opt => opt.Ignore())
                .ForMember(dest => dest.UserVote, opt => opt.Ignore());

            // Vote mapping
            CreateMap<VoteDto, Vote>()
                .ForMember(dest => dest.VoteId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
        }
    }
}
