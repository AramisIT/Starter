using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;

namespace Aramis.Loader.SolutionsInfoLoader
    {
    public static class SolutionListLoader
        {
        /// <summary>
        /// Загружает список доступных к запуску решений из файла с информацией о решениях
        /// </summary>
        /// <returns>Список решений</returns>
        public static List<SolutionInfo> LoadSolutionsList()
            {
            List<SolutionInfo> result = new List<SolutionInfo>();
            if ( !File.Exists(Program.SolutionsXMLFilePath) )
                {
                string.Format("Не найден файл списка решений: [{0}]", Program.SolutionsXMLFilePath).Error();
                }
            else
                {
                XElement doc = XDocument.Load(Program.SolutionsXMLFilePath).Element("root");
                List<XElement> solutionsList = doc.Elements("solution").ToList<XElement>();
                solutionsList.ForEach(solution =>
                {
                    result.Add(new SolutionInfo(solution.Element("Description").Value, solution.Element("ApplicationDirectory").Value));
                });
                }            
            return result;
            }
        }
    }
