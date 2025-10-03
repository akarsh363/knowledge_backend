// Profiles/MappingProfile.cs - Updated comment mappings
using AutoMapper;
using Project_Version1.Data;
using Project_Version1.DTOs;
using System.Linq;

namespace Project_Version1.Profiles
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // User mappings (unchanged)
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

            // Post mappings (unchanged)
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

            CreateMap<Post, PostBriefDto>()
                .ForMember(dest => dest.BodyPreview, opt => opt.MapFrom(src =>
                    string.IsNullOrEmpty(src.Body) ? string.Empty :
                    src.Body.Length > 200 ? src.Body.Substring(0, 200) + "..." : src.Body))
                .ForMember(dest => dest.UpvoteCount, opt => opt.MapFrom(src => src.UpvoteCount ?? 0))
                .ForMember(dest => dest.DownvoteCount, opt => opt.MapFrom(src => src.DownvoteCount ?? 0))
                .ForMember(dest => dest.CommentsCount, opt => opt.MapFrom(src =>
                    src.Comments != null ? src.Comments.Count : 0))
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.IsRepost, opt => opt.MapFrom(src => src.IsRepost))
                .ForMember(dest => dest.RepostedBy, opt => opt.MapFrom(src =>
                    src.Reposts.OrderByDescending(r => r.CreatedAt).FirstOrDefault().User.FullName));

            // Comment mappings - Updated to ignore vote fields (handled in service)
            CreateMap<CommentCreateDto, Comment>()
                .ForMember(dest => dest.CommentId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.UpvoteCount, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.DownvoteCount, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.UserVote, opt => opt.Ignore()); // Set in service

            CreateMap<Comment, CommentWithRepliesDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.Replies, opt => opt.Ignore()) // Built manually in service
                .ForMember(dest => dest.UpvoteCount, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.DownvoteCount, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.UserVote, opt => opt.Ignore()); // Set in service

            CreateMap<Comment, CommentDetailDto>()
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.IsDeleted, opt => opt.MapFrom(src => src.CommentText == "[Comment deleted]"))
                .ForMember(dest => dest.ReplyCount, opt => opt.MapFrom(src =>
                    src.InverseParentComment != null ? src.InverseParentComment.Count : 0))
                .ForMember(dest => dest.UpvoteCount, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.DownvoteCount, opt => opt.Ignore()) // Set in service
                .ForMember(dest => dest.UserVote, opt => opt.Ignore()); // Set in service

            // Vote mappings (unchanged)
            CreateMap<VoteDto, Vote>()
                .ForMember(dest => dest.VoteId, opt => opt.Ignore())
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

            // Tag and Department mappings (unchanged)
            CreateMap<Tag, TagDto>()
                .ForMember(dest => dest.TagId, opt => opt.MapFrom(src => src.TagId))
                .ForMember(dest => dest.TagName, opt => opt.MapFrom(src => src.TagName))
                .ForMember(dest => dest.DeptId, opt => opt.MapFrom(src => src.DeptId));

            CreateMap<Department, DepartmentDto>()
                .ForMember(dest => dest.DeptId, opt => opt.MapFrom(src => src.DeptId))
                .ForMember(dest => dest.DeptName, opt => opt.MapFrom(src => src.DeptName));

            CreateMap<Post, PostDetailDto>()
                .ForMember(dest => dest.Body, opt => opt.MapFrom(src => src.Body))  // Full body
                .ForMember(dest => dest.UpvoteCount, opt => opt.MapFrom(src => src.UpvoteCount ?? 0))
                .ForMember(dest => dest.DownvoteCount, opt => opt.MapFrom(src => src.DownvoteCount ?? 0))
                .ForMember(dest => dest.CommentsCount, opt => opt.MapFrom(src =>
                    src.Comments != null ? src.Comments.Count : 0))
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.User.FullName))
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Dept.DeptName))
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src =>
                    src.PostTags.Select(pt => pt.Tag.TagName).ToList()))
                .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments))
                .ForMember(dest => dest.IsRepost, opt => opt.MapFrom(src => src.IsRepost))
                .ForMember(dest => dest.RepostedBy, opt => opt.MapFrom(src =>
                    src.Reposts.OrderByDescending(r => r.CreatedAt).FirstOrDefault().User.FullName));

            CreateMap<Attachment, AttachmentDto>();
        }
    }
}