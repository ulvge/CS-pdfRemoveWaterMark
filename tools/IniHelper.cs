using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Debug.tools {
    public class IniHelper {
        const string INI_FILE_NAME = "./config.ini";

        /// <summary>
        /// 获取值
        /// </summary>
        /// <param name="section">段落名</param>
        /// <param name="key">键名</param>
        /// <param name="defval">读取异常是的缺省值</param>
        /// <param name="retval">键名所对应的的值，没有找到返回空值</param>
        /// <param name="size">返回值允许的大小</param>
        /// <param name="filepath">ini文件的完整路径</param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        private static extern int GetPrivateProfileString(
            string section,
            string key,
            string defval,
            StringBuilder retval,
            int size,
            string filepath);

        /// <summary>
        /// 写入
        /// </summary>
        /// <param name="section">需要写入的段落名</param>
        /// <param name="key">需要写入的键名</param>
        /// <param name="val">写入值</param>
        /// <param name="filepath">ini文件的完整路径</param>
        /// <returns></returns>
        [DllImport("kernel32.dll")]
        private static extern int WritePrivateProfileString(
            string section,
            string key,
            string val,
            string filepath);
        /// <summary>
        /// 获取数据
        /// </summary>
        /// <param name="section">段落名</param>
        /// <param name="key">键名</param>
        /// <param name="def">没有找到时返回的默认值</param>
        /// <param name="filename">ini文件完整路径</param>
        /// <returns></returns>
        public string getString(string section, string key, string def, string filename = INI_FILE_NAME) {
            StringBuilder sb = new StringBuilder(1024);
            GetPrivateProfileString(section, key, def, sb, 1024, filename);
            return sb.ToString();
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="section">段落名</param>
        /// <param name="key">键名</param>
        /// <param name="val">写入值</param>
        /// <param name="filename">ini文件完整路径</param>
        public void writeString(string section, string key, string val, string filename = INI_FILE_NAME) {
            WritePrivateProfileString(section, key, val, filename);
        }

        List<object> textBoxList = new List<object>();
        public List<object> FandAllTextBoxControls(Control control) {
            foreach(Control ct in control.Controls) {
                //调用AddControlInofToListBox方法获取控件信息
                if(ct is TextBox) {
                    textBoxList.Add((TextBox)ct);
                }
                if(ct is ComboBox) {
                    textBoxList.Add((ComboBox)ct);
                }
                if(ct is CheckBox) {
                    textBoxList.Add((CheckBox)ct);
                }
                //C#只遍历窗体的子控件，不遍历孙控件
                //当窗体上的控件有子控件时，需要用递归的方法遍历，才能全部列出窗体上的控件
                if(ct.HasChildren) {
                    FandAllTextBoxControls(ct);
                }
            }
            return textBoxList;
        }
        /// <summary>
        /// 从ini读取 参数，并加载到控件中
        /// </summary>
        /// <param name="form"></param>
        /// <param name="iniFileName">ini 的名称，如果缺失，有默认值 </param>
        public void IniLoader2Form(Form form) {
            // find all vals by read all para from ini, 
            // foreach all controls from From
            // fill paras into controls
            try {
                textBoxList.Clear();
                textBoxList = FandAllTextBoxControls(form);
                foreach(var tb in textBoxList) {
                    if(tb is TextBox) {
                        TextBox t = (TextBox)tb;
                        string readVal = getString(form.Text, t.Name, string.Empty, INI_FILE_NAME);
                        t.Text = readVal;
                    } else if(tb is ComboBox) {
                        ComboBox c = (ComboBox)tb;
                        string readVal = getString(form.Text, c.Name, string.Empty, INI_FILE_NAME);
                        c.Text = readVal;
                    } else if(tb is CheckBox) {
                        CheckBox c = (CheckBox)tb;
                        string readVal = getString(form.Text, c.Name, string.Empty, INI_FILE_NAME);
                        c.Checked = Boolean.Parse(readVal);
                    }
                }
            } catch(Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }
        public void IniUpdate2File(Form form, string iniFileName = INI_FILE_NAME) {
            textBoxList.Clear();
            textBoxList = FandAllTextBoxControls(form);
            foreach(var tb in textBoxList) {
                //write vals to ini
                if(tb is TextBox) {
                    TextBox t = (TextBox)tb;
                    writeString(form.Text, t.Name, t.Text.ToString(), iniFileName);
                } else if(tb is ComboBox) {
                    ComboBox c = (ComboBox)tb;
                    writeString(form.Text, c.Name, c.Text.ToString(), iniFileName);
                } else if(tb is CheckBox) {
                    CheckBox c = (CheckBox)tb;
                    writeString(form.Text, c.Name, c.Checked.ToString(), iniFileName);
                }
            }
        }
    }
}
