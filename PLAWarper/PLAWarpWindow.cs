using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NHSE.Injection;

namespace PLADumper
{
    public partial class PLAWarpWindow : Form
    {
        readonly long[] jumpsPos = new long[] { 0x42B2558, 0x88, 0x90, 0x1F0, 0x18, 0x80}; 
        readonly string jumpsPosExpr = "[[[[[[main+42B2558]+88]+90]+1F0]+18]+80]+90"; 
        static SysBot bot = new SysBot();
        static USBBot botUsb = new USBBot();

        List<Vector3> positions = new List<Vector3>();
        const string configName = "positions.txt";

        public PLAWarpWindow()
        {
            InitializeComponent();
            CultureInfo.CurrentCulture = new CultureInfo("en-US", false);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            if (File.Exists("config.txt"))
            {
                var ip = File.ReadAllText("config.txt");
                textBox1.Text = ip;
            }

            LoadAllAndUpdateUI();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                bot.Connect(textBox1.Text, 6000);
                groupBox1.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                botUsb.Connect();
                groupBox1.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            File.WriteAllText("config.txt", textBox1.Text);
        }

        private void MovePlayer(float x, float y)
        {
            int stepOffset = (int)numericUpDown1.Value;
            ulong ramOffset = getPcoordOfs();

            IRAMReadWriter sender = botUsb.Connected ? botUsb : bot;
            var bytes = sender.ReadBytes(ramOffset, 12, RWMethod.Absolute);
            float xn = BitConverter.ToSingle(bytes, 0);
            float yn = BitConverter.ToSingle(bytes, 8);
            xn += (x*stepOffset); yn += (y*stepOffset);

            sender.WriteBytes(BitConverter.GetBytes(xn), ramOffset, RWMethod.Absolute);
            sender.WriteBytes(BitConverter.GetBytes(yn), ramOffset + 8, RWMethod.Absolute);
        }

        private Vector3 GetPos()
        {
            ulong ramOffset = getPcoordOfs();
            IRAMReadWriter sender = botUsb.Connected ? botUsb : bot;
            var bytes = sender.ReadBytes(ramOffset, 12, RWMethod.Absolute);

            float xn = BitConverter.ToSingle(bytes, 0);
            float yn = BitConverter.ToSingle(bytes, 4);
            float zn = BitConverter.ToSingle(bytes, 8);

            return new Vector3() { x = xn, y = yn, z = zn };
        }

        private void SetPos(float x, float y, float z)
        {
            ulong ramOffset = getPcoordOfs();

            byte[] xb = BitConverter.GetBytes(x);
            byte[] yb = BitConverter.GetBytes(y);
            byte[] zb = BitConverter.GetBytes(z);

            var bytes = xb.Concat(yb).Concat(zb);

            IRAMReadWriter sender = botUsb.Connected ? botUsb : bot;
            sender.WriteBytes(bytes.ToArray(), ramOffset, RWMethod.Absolute);
        }

        private ulong getPcoordOfs()
        {
            if (botUsb.Connected)
                return PointerUtil.GetPointerAddressFromExpression(botUsb, jumpsPosExpr);
            else
                return bot.FollowMainPointer(jumpsPos) + 0x90;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            MovePlayer(0, -1);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            MovePlayer(0, 1);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            MovePlayer(1, 0);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            MovePlayer(-1, 0);
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        public struct Vector3
        {
            public float x, y, z;

            public override string ToString()
            {
                return $"{x},{y},{z}";
            }

            public static Vector3 FromString(string s)
            {
                var spl = s.Split(',');
                Vector3 v = new Vector3();
                v.x = float.Parse(spl[0]);
                v.y = float.Parse(spl[1]);
                v.z = float.Parse(spl[2]);

                return v;
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            SaveNewValue();
        }

        private void SaveNewValue()
        {
            var pos = GetPos();
            positions.Add(pos);
            SaveAllAndUpdateUI();

            listBox1.SelectedIndex = listBox1.Items.Count - 1;
        }

        private void LoadAllAndUpdateUI()
        {
            if (File.Exists(configName))
            {
                var lines = File.ReadAllLines(configName);
                foreach (var line in lines)
                    positions.Add(Vector3.FromString(line));
            }

            UpdateUI();
        }

        private void SaveAllAndUpdateUI()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var pos in positions)
                sb.AppendLine(pos.ToString());

            File.WriteAllText(configName, sb.ToString());

            UpdateUI();
        }

        private void UpdateUI()
        {
            listBox1.SelectedIndex = -1;
            listBox1.Items.Clear();
            foreach (var pos in positions)
                listBox1.Items.Add(pos);
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                var toSend = (Vector3)listBox1.SelectedItem;
                SetPos(toSend.x, toSend.y, toSend.z);
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex > -1)
            {
                positions.RemoveAt(listBox1.SelectedIndex);
                SaveAllAndUpdateUI();
            }
        }
    }
}
