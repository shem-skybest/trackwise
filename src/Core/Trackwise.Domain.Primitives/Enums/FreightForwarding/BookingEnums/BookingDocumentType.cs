using System;
using System.Collections.Generic;
using System.Text;

namespace Trackwise.Domain.Primitives.Enums.FreightForwarding.BookingEnums;

public enum BookingDocumentType
{
    Kyc = 1,
    ImportPermit = 2,
    ExportPermit = 3,
    DangerousGoodsDeclaration = 4,
    ColdChainCertificate = 5,
    InsuranceCertificate = 6,
    CommercialInvoice = 7,
    PackingList = 8,
    Other = 99
}
