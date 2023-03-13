using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace STS_Bcut.src.Common
{
    public class STSTask
    {
		public STSTask(string tip) { 
			Tip=tip;
			Percentage= 0;
		}

		private string tip;
		/// <summary>
		/// 提示
		/// </summary>
		public string Tip
		{
			get { return tip; }
			set { tip = value; }
		}

		private int percentage;
		/// <summary>
		/// 进度条的百分比
		/// </summary>
        public int Percentage
        {
			get { return percentage; }
			set { percentage = value; }
		}


	}
}
