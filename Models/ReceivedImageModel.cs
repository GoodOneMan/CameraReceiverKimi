using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CameraReceiverKimi.Models
{
    /// <summary>
    /// Модель полученных данных: изображение + комментарий + информация об отправителе.
    /// </summary>
    public class ReceivedImageModel
    {
        /// <summary>Байты JPEG-изображения</summary>
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();

        /// <summary>Текстовый комментарий</summary>
        public string Comment { get; set; } = string.Empty;

        /// <summary>IP-адрес Android-клиента</summary>
        public string SenderIp { get; set; } = string.Empty;

        /// <summary>Порт, с которого отправлены данные</summary>
        public int SenderPort { get; set; }
    }
}
