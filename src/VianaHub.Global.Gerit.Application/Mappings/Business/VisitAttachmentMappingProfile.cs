using AutoMapper;
using System.Globalization;
using VianaHub.Global.Gerit.Application.Dtos.Request.Business.VisitAttachment;
using VianaHub.Global.Gerit.Application.Dtos.Response.Business.VisitAttachment;
using VianaHub.Global.Gerit.Domain.Entities.Business;
using VianaHub.Global.Gerit.Domain.Services;

namespace VianaHub.Global.Gerit.Application.Mappings.Business;

public class VisitAttachmentsMappingProfile : Profile
{
    public VisitAttachmentsMappingProfile()
    {
        CreateMap<VisitAttachmentsEntity, VisitAttachmentResponse>()
            .ForMember(dest => dest.FileTypeName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.FileType?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.MimeType, opt => opt.MapFrom(src => src.FileType == null ? null : src.FileType.MimeType));

        CreateMap<VisitAttachmentsEntity, VisitAttachmentDetailResponse>()
            .ForMember(dest => dest.FileTypeName, opt => opt.MapFrom((src, _, _, _) =>
                TranslationResolver.Resolve(src.FileType?.Translations, CultureInfo.CurrentCulture.Name, t => t.LanguageCode, t => t.Name)))
            .ForMember(dest => dest.MimeType, opt => opt.MapFrom(src => src.FileType == null ? null : src.FileType.MimeType))
            .ForMember(dest => dest.FormattedFileSize, opt => opt.MapFrom(src => src.GetFormattedFileSize()));

        CreateMap<CreateVisitAttachmentRequest, VisitAttachmentsEntity>();
        CreateMap<UpdateVisitAttachmentRequest, VisitAttachmentsEntity>();
    }
}
