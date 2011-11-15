using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aramis.Loader.SolutionUpdate
    {
    public class UpdatingInfo
        {
        /// <summary>
        /// Полное РЕАЛЬНОЕ имя файла который будет загружен(включая информацию о пути в который он будет загружен)
        /// </summary>
        public string FileName
            {
            get;
            set;
            }

        /// <summary>
        /// Имя, которое получит текущий файл в процессе загрузки(имя включает информацию о пути). 
        /// Это имя будет изменено на имя из FileName в процессе переименования при окончании обновления.
        /// </summary>
        public string UpdateFileName
            {
            get;
            set;
            }

        /// <summary>
        /// Размер файла
        /// </summary>
        public long FileSize
            {
            get;
            set;
            }

        /// <summary>
        /// Хэш файла
        /// </summary>
        public string FileHash
            {
            get;
            set;
            }
        }
    }
