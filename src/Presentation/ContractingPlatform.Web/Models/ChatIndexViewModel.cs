using ContractingPlatform.Application.DTOs.Chat;
using ContractingPlatform.Application.DTOs.Contracts;

namespace ContractingPlatform.Web.Models;

public class ChatIndexViewModel
{
    public List<ChatConversationDto> Conversations { get; set; } = new();
    public int? ActiveContractId { get; set; }
    public ContractDetailsDto? ActiveContract { get; set; }
    public List<ChatMessageDto> ActiveMessages { get; set; } = new();
    public string CurrentUserId { get; set; } = string.Empty;
}
