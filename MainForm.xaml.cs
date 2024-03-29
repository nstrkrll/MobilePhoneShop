﻿using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace MobilePhoneShop
{
    public partial class MainForm : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_STYLE = -16;

        private const int WS_MAXIMIZEBOX = 0x10000; //maximize button
        private IntPtr _windowHandle;
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            _windowHandle = new WindowInteropHelper(this).Handle;
            DisableMaximizeButton();
        }

        protected void DisableMaximizeButton()
        {
            if (_windowHandle == null)
                throw new InvalidOperationException("The window has not yet been completely initialized");

            SetWindowLong(_windowHandle, GWL_STYLE, GetWindowLong(_windowHandle, GWL_STYLE) & ~WS_MAXIMIZEBOX);
        }

        //---------------------------------------------------------------------------------------------------------------

        AccessToDB acdb = new AccessToDB();
        AppContext apc = new AppContext();
        List<Phone> phones;
        List<Phone> searchReq = new List<Phone>();
        public List<Phone> phonesInBin;
        private double angle = 0;
        private DispatcherTimer timer;
        public MainForm()
        {
            InitializeComponent();
            phones = apc.phones.ToList();
            Phones_ListBox.ItemsSource = phones;
            phonesInBin = new List<Phone>();
            this.SourceInitialized += MainWindow_SourceInitialized;
            //-------------
            // анимация
            ObjReader CurrentHelixObjReader = new ObjReader();
            Model3DGroup MyModel = CurrentHelixObjReader.Read("C:\\Users\\neste\\Desktop\\MobilePhoneShop\\Images\\handy.obj");
            foo.Content = MyModel;
            this.timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
            this.timer.Tick += Timer_Tick;
            this.timer.Start();
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (this.angle >= 360)
            {
                this.angle = 0;
            }
            else
            {
                //Nothing to do
            }
            this.angle = this.angle + 0.5;
            //You can adapt the code if you have many children 
            if (foo.Transform is RotateTransform3D rotateTransform3 && rotateTransform3.Rotation is AxisAngleRotation3D rotation)
            {
                rotation.Angle = this.angle;
            }
            else
            {
                ///Initialize the Transform (I didn't do it in my example but you could do this initialization in <see Load3dModel/>)
                foo.Transform = new RotateTransform3D()
                {
                    Rotation = new AxisAngleRotation3D()
                    {
                        Axis = new Vector3D(0, 0, 1),
                        Angle = this.angle,
                    }
                };
            }
        }

        private void Profile_Button_Click(object sender, RoutedEventArgs e)
        {
            ProfileWindow pfw = new ProfileWindow();
            pfw.Owner = this;
            Hide();
            pfw.ShowDialog();
        }

        private void RecycleBin_Button_Click(object sender, RoutedEventArgs e)
        {
            RecycleBin recycleBin = new RecycleBin(phonesInBin);
            recycleBin.Owner = this;
            Hide();
            recycleBin.ShowDialog();
        }

        private void Search_TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            searchReq.Clear();
            Phones_ListBox.ItemsSource = phones;
            foreach (Phone phone in Phones_ListBox.Items)
            {
                if (phone.Models.ToLower().Contains(Search_TextBox.Text.ToLower()))
                {
                    searchReq.Add(phone);
                }
            }
            Phones_ListBox.ItemsSource = null;
            Phones_ListBox.Items.Clear();
            Phones_ListBox.ItemsSource = searchReq;
            if (Search_TextBox.Text.Length == 0)
            {
                Phones_ListBox.ItemsSource = phones;
            }
        }

        private void Phones_ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Phones_ListBox.SelectedItem != null)
            {
                myView.Visibility = Visibility.Hidden;
                Phone selectedPhone = (Phone)Phones_ListBox.SelectedItem;
                Phone_Image.Source = selectedPhone.getImage;
                PhoneModel_TextBlock.Text = "Модель: " + selectedPhone.Models + " " + selectedPhone.RamCapacity + "/" + selectedPhone.RomCapacity;
                PhoneOS_TextBlock.Text = "ОС: " + apc.os.Where(oss => oss.osID == selectedPhone.OsID).FirstOrDefault().Name;
                PhoneDisplay_TextBlock.Text = "Дисплей: " + apc.displayTeches.Where(display => display.displayTechID == selectedPhone.DisplayTechID).FirstOrDefault().Type;
                PhoneCameras_TextBlock.Text = "Камеры: основная: " + selectedPhone.MainCameraRes + " МП | фронтальная: " + selectedPhone.FrontCameraRes + " МП";
                PhoneAccumulator_TextBlock.Text = "Аккумулятор: " + apc.accs.Where(acc => acc.accID == selectedPhone.AccID).FirstOrDefault().Type + " " + selectedPhone.AccCapacity + " мАч";
                AddToBin_Button.Visibility = Visibility.Visible;
            }
        }

        private void AddToBin_Button_Click(object sender, RoutedEventArgs e)
        {
            Phone selectedPhone = (Phone)Phones_ListBox.SelectedItem;
            phonesInBin.Add(selectedPhone);
            Phone_Image.Source = null;
            PhoneModel_TextBlock.Text = null;
            PhoneOS_TextBlock.Text = null;
            PhoneDisplay_TextBlock.Text = null;
            PhoneCameras_TextBlock.Text = null;
            PhoneAccumulator_TextBlock.Text = null;
            AddToBin_Button.Visibility = Visibility.Hidden;
            Phones_ListBox.UnselectAll();
            myView.Visibility = Visibility.Visible;
            MessageBox.Show("Товар добавлен в корзину!");
        }

        // --------------------------------

        public Model3DGroup Load(string path)
        {
            if (path == null)
            {
                return null;
            }

            Model3DGroup model = null;
            string ext = System.IO.Path.GetExtension(path).ToLower();
            switch (ext)
            {
                case ".3ds":
                    {
                        var r = new HelixToolkit.Wpf.StudioReader();
                        model = r.Read(path);
                        break;
                    }

                case ".lwo":
                    {
                        var r = new HelixToolkit.Wpf.LwoReader();
                        model = r.Read(path);

                        break;
                    }

                case ".obj":
                    {
                        var r = new HelixToolkit.Wpf.ObjReader();
                        model = r.Read(path);
                        break;
                    }

                case ".objz":
                    {
                        var r = new HelixToolkit.Wpf.ObjReader();
                        model = r.ReadZ(path);
                        break;
                    }

                case ".stl":
                    {
                        var r = new HelixToolkit.Wpf.StLReader();
                        model = r.Read(path);
                        break;
                    }

                case ".off":
                    {
                        var r = new HelixToolkit.Wpf.OffReader();
                        model = r.Read(path);
                        break;
                    }

                default:
                    throw new InvalidOperationException("File format not supported.");
            }
            return model;
        }
    }
}
