using AutoMapper;
using BankManagement.Application.DTO;
using BankManagement.Domain;
using BankManagement.Domain.Entities;

namespace BankManagement.Application;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Branch, BranchDto>().ReverseMap();
        CreateMap<BankTransaction, TransactionDto>();
        CreateMap<Loan, LoanDto>()
            .ForCtorParam(nameof(LoanDto.CustomerProfileId), cfg => cfg.MapFrom(src => src.CustomerProfileId));
        CreateMap<CustomerProfile, CustomerDto>()
            .ForCtorParam(nameof(CustomerDto.IdentityUserId), cfg => cfg.MapFrom(src => src.IdentityUserId))
            .ForCtorParam(nameof(CustomerDto.CreatedByTellerId), cfg => cfg.MapFrom(src => src.CreatedByTellerId));
    }
}
