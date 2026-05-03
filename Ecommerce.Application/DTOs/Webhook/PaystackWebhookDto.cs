using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ecommerce.Application.DTOs.Webhook;

public class PaystackWebhookDto
{
    public string Event { get; set; }
    public PaystackData Data { get; set; }
}

public class PaystackData
{
    public string Reference { get; set; }
}
