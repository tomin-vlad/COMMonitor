using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Windows.Forms;

namespace COMMonitor
{
    public partial class Form : System.Windows.Forms.Form
    {
        string[] oldPorts; // Глобальная переменная, хранящая список COM-портов с каждого предыдущего цикла опроса

        public Form()
        {
            InitializeComponent(); // Инициализация компонентов
            notifyIcon.Text = Text; // Подпись иконки в трее - заголовок формы
        }

        /*
         * Событие загрузки формы
         */
        private void Form_Load(object sender, EventArgs e)
        {
            oldPorts = SerialPort.GetPortNames(); // Заносим в глобальную переменную текущий список COM-портов
            WindowState = FormWindowState.Minimized; // Сворачиваем окно
            backgroundWorker.RunWorkerAsync(); // Запускаем фоновый поток

            Timer timerStatus = new Timer(); // Создаем таймер
            timerStatus.Interval = 100; // Устанавливаем интервал таймера в 100 мс
            timerStatus.Tick += new EventHandler(timerStatus_Tick); // По завершении интервала - новый интервал
            timerStatus.Start(); // Стартуем таймер
        }

        /*
         * Событие изменения размеров формы
         */ 
        private void Form_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) // Если окно было свернуто
            {
                ShowInTaskbar = false; // Отключаем показ иконки на панели задач
                notifyIcon.Visible = true; // Включаем показ иконки в трее
            }
        }

        /*
         * Событие клика по кнопке "Пуск" формы
         */
        private void buttonStart_Click(object sender, EventArgs e)
        {
            oldPorts = SerialPort.GetPortNames(); // Заносим в глобальную переменную текущий список COM-портов
            WindowState = FormWindowState.Minimized; // Сворачиваем окно
            backgroundWorker.RunWorkerAsync(); // Запускаем фоновый поток
        }

        /*
         *  Событие клика по кнопке "Стоп" формы
         */
        private void buttonStop_Click(object sender, EventArgs e)
        {
            backgroundWorker.CancelAsync(); // Останавливаем фоновый поток
        }

        /*
         * Событие двойного клика по иконке в трее
         */
        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ShowInTaskbar = true; // Включаем показ иконки на панели задач
            WindowState = FormWindowState.Normal; // Разворачиваем окно
        }

        /*
         * Событие клика по пункту "Пуск" контекстного меню
         */
        private void toolStripMenuItemStart_Click(object sender, EventArgs e)
        {
            oldPorts = SerialPort.GetPortNames(); // Заносим в глобальную переменную текущий список COM-портов
            backgroundWorker.RunWorkerAsync(); // Запускаем фоновый поток
        }

        /*
         * Событие клика по пункту "Стоп" контекстного меню
         */
        private void toolStripMenuItemStop_Click(object sender, EventArgs e)
        {
            backgroundWorker.CancelAsync(); // Останавливаем фоновый поток
            WindowState = FormWindowState.Normal; // Разворачиваем окно
        }

        /*
         * Событие клика по пункту "Выход" контекстного меню
         */
        private void toolStripMenuItemExit_Click(object sender, EventArgs e)
        {
            backgroundWorker.CancelAsync(); // Останавливаем фоновый поток
            Application.Exit(); // Закрываем программу
        }

        /*
         * Интервал таймера, определяюшего текущий статус работы программы
         */
        private void timerStatus_Tick(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy) // Запущен ли фоновый поток
            {
                labelMode.Text = "Работает"; // Выводим текстовый статус
                labelMode.ForeColor = Color.Green; // Делаем цвет статуса зеленым
                buttonStart.Enabled = contextMenuStrip.Items[0].Enabled = false; // Отпключаем кнопку формы и пункт контекстного меню "Пуск"
                buttonStop.Enabled = contextMenuStrip.Items[1].Enabled = true; // Включаем кнопку формы и пункт контекстного меню "Стоп"
            }
            else
            {
                labelMode.Text = "Стоп"; // Выводим текстовый статус
                labelMode.ForeColor = Color.Red; // Делаем цвет статуса красным
                buttonStart.Enabled = contextMenuStrip.Items[0].Enabled = true; // Включаем кнопку формы и пункт контекстного меню "Пуск"
                buttonStop.Enabled = contextMenuStrip.Items[1].Enabled = false; // Отключаем кнопку формы и пункт контекстного меню "Стоп"
            }
        }

        /*
         * Работа в фоновом потоке
         */
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true) // Бесконечный цикл
            {
                if (backgroundWorker.CancellationPending) return; // Если была подана команда на остановку фонового процесса, прерываем цикл

                string[] newPorts = SerialPort.GetPortNames(); // Получаем текущий список COM-портов
                var addPorts = newPorts.Except(oldPorts); // Сравниваем текущий список COM-портов с предыдущим (получаем добавленные COM-порты)
                var delPorts = oldPorts.Except(newPorts); // Сравниваем предыдущий список COM-портов с текущим (получаем удаленные COM-порты)

                if (addPorts.Count() > 0) // Если кол-во добавленных COM-портов больше 0
                {
                    notifyIcon.BalloonTipIcon = ToolTipIcon.Info; // Задаем информационную иконку уведомлению
                    notifyIcon.BalloonTipText = "Порт: " + String.Join(", ", addPorts.ToArray()); // Задаем текст уведомления - список добавленных COM-портов
                    notifyIcon.BalloonTipTitle = "Подключено"; // Задаем заголовок уведомления
                    notifyIcon.ShowBalloonTip(3); // Показываем уведомление на 3 сек
                }

                if (delPorts.Count() > 0) // Если кол-во удаленных COM-портов больше 0
                {
                    notifyIcon.BalloonTipIcon = ToolTipIcon.Info; // Задаем информационную иконку уведомлению
                    notifyIcon.BalloonTipText = "Порт: " + String.Join(", ", delPorts.ToArray()); // Задаем текст уведомления - список удаленных COM-портов
                    notifyIcon.BalloonTipTitle = "Отключено"; // Задаем заголовок уведомления
                    notifyIcon.ShowBalloonTip(3); // Показываем уведомление на 3 сек
                }
                oldPorts = newPorts; // Обновляем глобальную переменную списком текущих COM-портов
                System.Threading.Thread.Sleep(100); // Задаем паузу в 100 мс
            }
        }
    }
}
