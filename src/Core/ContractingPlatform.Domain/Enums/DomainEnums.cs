namespace ContractingPlatform.Domain.Enums;

public enum UserType
{
    Client = 1,
    Contractor = 2,
    Admin = 3
}

public enum VerificationStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum ProjectStatus
{
    Draft = 0,
    OpenForBids = 1,
    UnderReview = 2,
    InProgress = 3,
    Completed = 4,
    Cancelled = 5
}

public enum BidStatus
{
    Submitted = 1,
    Accepted = 2,
    Rejected = 3,
    Withdrawn = 4
}

public enum MilestoneStatus
{
    Pending = 1,
    InProgress = 2,
    SubmittedForReview = 3,
    Approved = 4,
    Paid = 5
}

public enum PaymentStatus
{
    Pending = 1,
    HeldInEscrow = 2,
    ReleasedToContractor = 3,
    RefundedToClient = 4,
    Failed = 5
}

public enum PaymentMethod
{
    CreditCard = 1,
    Mada = 2,
    ApplePay = 3,
    BankTransfer = 4,
    Wallet = 5
}
