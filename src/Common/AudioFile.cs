using Prism.Mvvm;

namespace STS_Bcut.src.Common
{
    public class AudioFile : BindableBase
    {
        private bool isselected;
        /// <summary>
        /// 是否被选中
        /// </summary>
        public bool IsSelected
        {
            get { return isselected; }
            set { isselected = value; RaisePropertyChanged(); }
        }


        private string fullname;
        /// <summary>
        /// 音频文件全称
        /// </summary>
        public string FullName
        {
            get { return fullname; }
            set { fullname = value; }
        }

        private string fullpath;
        /// <summary>
        /// 绝对路径
        /// </summary>
        public string FullPath
        {
            get { return fullpath; }
            set { fullpath = value; }
        }

    }
}
