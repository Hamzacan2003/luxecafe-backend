using System;
using System.Collections.Generic;
using System.Text;

namespace CafeApp.Business.Services.Abstract
{
    public interface IQrCodeService
    {
        byte[] GenerateQrCode(string text);
    }
}
