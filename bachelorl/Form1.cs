using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Xml.Xsl;

namespace bachelorl
{
    public partial class Form1 : Form
    {
        public double[] qs = new double[4];  // q start params 
        public double[] qe = new double[4];  // q end paramss
        public double[] ws = new double[3];  // w start params 
        public double[] we = new double[3];  // w end paramss

        public double[] xs = new double[7];  // x start params
        public double[] xe = new double[7];  // x end paramss

        public double[] J = new double[3];
        public double[] p = new double[7];

        public double[] Mg = new double[3];
        public double miu = 3.9660 * Math.Pow(10, 14);
        public double Ro = 6371000 + 35000000;

        public int[][] u = new int[8][];

        public Form1()
        {
            InitializeComponent();

            for (int i = 0; i < qs.Length; i++)
            {
                q_startGrid.Rows.Add(i + 1, 0.01);
            }
            for (int i = 0; i < qe.Length; i++)
            {
                q_endGrid.Rows.Add(i + 1, 0.01);
            }
            for (int i = 0; i < ws.Length; i++)
            {
                w_startGrid.Rows.Add(i + 1, 0.01);
            }
            for (int i = 0; i < we.Length; i++)
            {
                w_endGrid.Rows.Add(i + 1, 0.01);
            }



            u[0] = new int[] { 1, 1, -1 };
            u[2] = new int[] { 1, -1, -1 };
            u[3] = new int[] { -1, -1, -1 };
            u[1] = new int[] { 1, 1, 1 };
            u[4] = new int[] { -1, -1, 1 };
            u[5] = new int[] { -1, 1, 1 };
            u[6] = new int[] { 1, -1, 1 };
            u[7] = new int[] { -1, 1, -1 };

            inertionDgv.Rows.Add(1, 150);
            inertionDgv.Rows.Add(1, 165);
            inertionDgv.Rows.Add(1, 175);


            for (int i = 0; i < 7; i++)
            {
                pGrid.Rows.Add(i + 1, 1);
            }
        }

        public double[] readValuesFromGrid(DataGridView dgv)
        {
            double[] data = new double[dgv.Rows.Count];
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                data[i] = Convert_.toDouble(dgv.Rows[i].Cells[1].Value);
            }
            return data;
        }

        public void readAndTransferQWtoX()
        {
            qs = readValuesFromGrid(q_startGrid);
            qe = readValuesFromGrid(q_endGrid);
            ws = readValuesFromGrid(w_startGrid);
            we = readValuesFromGrid(w_endGrid);
            J = readValuesFromGrid(inertionDgv);
            p = readValuesFromGrid(pGrid);

            for (int i = 0; i < qs.Length; i++)
            {
                xs[i] = qs[i];
            }
            for (int i = 0; i < ws.Length; i++)
            {
                xs[i + qs.Length] = ws[i];
            }


            for (int i = 0; i < qs.Length; i++)
            {
                xe[i] = qe[i];
            }
            for (int i = 0; i < ws.Length; i++)
            {
                xe[i + qe.Length] = we[i];
            }



        }

        public void checkQ()
        {
            if (qs.Sum() > 1)
                MessageBox.Show("У вас помилка при введенні параметрів q початкових");
            if (qe.Sum() > 1)
                MessageBox.Show("У вас помилка при введенні параметрів q кінцевих");
        }



        private void mainCalsFunction(object sender, EventArgs e)
        {
            readAndTransferQWtoX();
            checkQ();


            double Omega = 0.01;

            double[,] A = new double[3, 3]
            {
                { 2* (xs[3]*xs[3] + xs[0]*xs[0]) - 1, 2*(xs[0]*xs[1] + xs[2]*xs[3]), 2*(xs[0]*xs[2] + xs[3]*xs[1])  },
                { 2*(xs[0]*xs[1] + xs[3]*xs[2]),  (xs[3]*xs[3] + xs[1]*xs[1]) - 1 ,  2*(xs[1]*xs[2] + xs[3]*xs[0])  },
                { 2*(xs[0]*xs[2] + xs[3]*xs[1]),  2*(xs[1]*xs[2] + xs[3]*xs[0]), (xs[3]*xs[3] + xs[2]*xs[2]) - 1  }
            };



            Mg[0] = ((3 * miu) / Ro) * (-J[1] + J[2]) * A[1, 2] * A[2, 2];
            Mg[1] = ((3 * miu) / Ro) * (J[0] - J[2]) * A[0, 2] * A[2, 2];
            Mg[2] = ((3 * miu) / Ro) * (-J[0] + J[1]) * A[1, 2] * A[0, 2];




            double HUmax = -100000000000;
            int bestUontheLastIteration = -1;

            for (int i = 0; i < 7; i++)
            {
                double tmpSum = 0;
                tmpSum += p[0] * system1Equations.eq1(xs, Omega);
                tmpSum += p[1] * system1Equations.eq2(xs, Omega);
                tmpSum += p[2] * system1Equations.eq3(xs, Omega);
                tmpSum += p[3] * system1Equations.eq4(xs, Omega);


                tmpSum += p[4] * system1Equations.eq5(xs, J, u[i], Mg);
                tmpSum += p[5] * system1Equations.eq6(xs, J, u[i], Mg);
                tmpSum += p[6] * system1Equations.eq7(xs, J, u[i], Mg);

                if (tmpSum > HUmax)
                {
                    HUmax = tmpSum;
                    bestUontheLastIteration = i;
                }
            }



            int AllTime = Convert.ToInt32(timeBox.Text);
            int N = Convert.ToInt32(Nbox.Text);
            double currentTime = 0;
            double step = (double)AllTime / (double)N;


            double[] xsN_t = new double[7] { xs[0], xs[1], xs[2], xs[3], xs[4], xs[5], xs[6] };
            double[] xsN_t_minus_odin = new double[7] { xs[0], xs[1], xs[2], xs[3], xs[4], xs[5], xs[6] };



            double[] p_t = new double[7] { p[0], p[1], p[2], p[3], p[4], p[5], p[6] };
            double[] pt_minus_one = new double[7] { p[0], p[1], p[2], p[3], p[4], p[5], p[6] };



            List<double[]> xHistory = new List<double[]>();
            List<double[]> pHistory = new List<double[]>();
            List<int> bestiters = new List<int>();


            int counter = 0;
            while (currentTime < AllTime) // ДАЛІ - метод ЕЙЛЕРА (цей цикл)
            {
                counter++;
                xHistory.Add(new double[7] { xsN_t[0], xsN_t[1], xsN_t[2], xsN_t[3], xsN_t[4], xsN_t[5], xsN_t[6] });
                pHistory.Add(new double[7] { p_t[0], p_t[1], p_t[2], p_t[3], p_t[4], p_t[5], p_t[6] });

                xsN_t[0] = xsN_t_minus_odin[0] + step * system1Equations.eq1(xsN_t_minus_odin, Omega);
                xsN_t[1] = xsN_t_minus_odin[1] + step * system1Equations.eq2(xsN_t_minus_odin, Omega);
                xsN_t[2] = xsN_t_minus_odin[2] + step * system1Equations.eq3(xsN_t_minus_odin, Omega);
                xsN_t[3] = xsN_t_minus_odin[3] + step * system1Equations.eq4(xsN_t_minus_odin, Omega);

                xsN_t[4] = xsN_t_minus_odin[4] + step * system1Equations.eq5(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);
                xsN_t[5] = xsN_t_minus_odin[5] + step * system1Equations.eq6(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);
                xsN_t[6] = xsN_t_minus_odin[6] + step * system1Equations.eq7(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);

                p_t[0] = pt_minus_one[0] + step * pPointEquations.eq1(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro);
                p_t[1] = pt_minus_one[1] + step * pPointEquations.eq2(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro);
                p_t[2] = pt_minus_one[2] + step * pPointEquations.eq3(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro);
                p_t[3] = pt_minus_one[3] + step * pPointEquations.eq4(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro);
                p_t[4] = pt_minus_one[4] + step * pPointEquations.eq5(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro);
                p_t[5] = pt_minus_one[5] + step * pPointEquations.eq6(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro);
                p_t[6] = pt_minus_one[6] + step * pPointEquations.eq7(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro);


                //тут ми виводимо наші дані в табличку і саме тут видноо що вони погані (безкінечність або НЕчисло)
                pgridOutput.Rows.Add(counter, p_t[0], p_t[1], p_t[2], p_t[3], p_t[4], p_t[5], p_t[6]);
                xgridOutput.Rows.Add(counter, xsN_t[0], xsN_t[1], xsN_t[2], xsN_t[3], xsN_t[4], xsN_t[5], xsN_t[6]);


                HUmax = -100000000000;
                bestUontheLastIteration = 1;

                for (int i = 0; i < 7; i++)
                {
                    double tmpSum = 0;
                    tmpSum += p_t[0] * system1Equations.eq1(xsN_t, Omega);
                    tmpSum += p_t[1] * system1Equations.eq2(xsN_t, Omega);
                    tmpSum += p_t[2] * system1Equations.eq3(xsN_t, Omega);
                    tmpSum += p_t[3] * system1Equations.eq4(xsN_t, Omega);


                    tmpSum += p_t[4] * system1Equations.eq5(xsN_t, J, u[i], Mg);
                    tmpSum += p_t[5] * system1Equations.eq6(xsN_t, J, u[i], Mg);
                    tmpSum += p_t[6] * system1Equations.eq7(xsN_t, J, u[i], Mg);

                    if (tmpSum > HUmax)
                    {
                        HUmax = tmpSum;
                        bestUontheLastIteration = i;
                    }
                }
                bestiters.Add(bestUontheLastIteration);







                currentTime += step;

                xsN_t_minus_odin = new double[7] { xsN_t[0], xsN_t[1], xsN_t[2], xsN_t[3], xsN_t[4], xsN_t[5], xsN_t[6] };
                pt_minus_one = new double[7] { p_t[0], p_t[1], p_t[2], p_t[3], p_t[4], p_t[5], p_t[6] };
            }



            Random R = new Random();

            double[] gradient = new double[8] { 0, 0, 0, 0, 0, 0, 0, 0 };


            while (true)
            {
                //нев'язки
                // рахуємо з кінця

                for (int activeB = 0; activeB < 7; activeB++)
                {
                    gradient[activeB] = 0;
                    double delta = R.NextDouble() / 1000000;
                    double F_with_delta = 0;
                    double F_without_delta = 0;


                    F_with_delta += activeB != 0 ? p[0] * system1Equations.eq1(xsN_t, Omega) : (p[0] + delta) * system1Equations.eq1(xsN_t, Omega);
                    F_with_delta += activeB != 1 ? p[1] * system1Equations.eq2(xsN_t, Omega) : (p[1] + delta) * system1Equations.eq2(xsN_t, Omega);
                    F_with_delta += activeB != 2 ? p[2] * system1Equations.eq3(xsN_t, Omega) : (p[2] + delta) * system1Equations.eq3(xsN_t, Omega);
                    F_with_delta += activeB != 3 ? p[3] * system1Equations.eq4(xsN_t, Omega) : (p[3] + delta) * system1Equations.eq4(xsN_t, Omega);
                    F_with_delta += activeB != 4 ? p[4] *
                        system1Equations.eq5(xsN_t, J, u[bestUontheLastIteration], Mg) : (p[4] + delta) * system1Equations.eq5(xsN_t, J, u[bestUontheLastIteration], Mg);
                    F_with_delta += activeB != 5 ? p[5] *
                        system1Equations.eq6(xsN_t, J, u[bestUontheLastIteration], Mg) : (p[4] + delta) * system1Equations.eq6(xsN_t, J, u[bestUontheLastIteration], Mg);
                    F_with_delta += activeB != 6 ? p[6] *
                        system1Equations.eq7(xsN_t, J, u[bestUontheLastIteration], Mg) : (p[4] + delta) * system1Equations.eq7(xsN_t, J, u[bestUontheLastIteration], Mg);
                    F_with_delta = Math.Pow(gradient[activeB] - 1, 2);
                    for (int i = 0; i < xe.Length; i++)
                    {
                        F_with_delta += Math.Pow(xe[i] - xsN_t[i], 2);
                    }

                    F_without_delta += p[0] * system1Equations.eq1(xsN_t, Omega);
                    F_without_delta += p[1] * system1Equations.eq2(xsN_t, Omega);
                    F_without_delta += p[2] * system1Equations.eq3(xsN_t, Omega);
                    F_without_delta += p[3] * system1Equations.eq4(xsN_t, Omega);
                    F_without_delta += p[4] * system1Equations.eq5(xsN_t, J, u[bestUontheLastIteration], Mg);
                    F_without_delta += p[5] * system1Equations.eq6(xsN_t, J, u[bestUontheLastIteration], Mg);
                    F_without_delta += p[6] * system1Equations.eq7(xsN_t, J, u[bestUontheLastIteration], Mg);
                    F_without_delta = Math.Pow(gradient[activeB] - 1, 2);

                    for (int i = 0; i < xe.Length; i++)
                    {
                        F_without_delta += Math.Pow(xe[i] - xsN_t[i], 2);
                    }
                    //Ось тут - потрібно для градієнта орахувати похідну по часові, питання в тому, що додати до цілочисельного часу якусь безкінечно малу дельту - нереально нараді
                    // або близько до цього. Яку дельту додавати? Як при цьому змінювати P та Х?  (проблемка в тому що у нас t - дискретний час.)

                    double difference_between_with_delta_and_without_delta = F_with_delta - F_without_delta;


                    gradient[activeB] = difference_between_with_delta_and_without_delta / delta;
                }
                break;
            }
            MessageBox.Show($"{bestUontheLastIteration} - найкраща ітер");
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
    class system1Equations
    {
        public static double eq1(double[] x, double Omega)
        {
            double result = x[6] * x[1] + (-x[5] + Omega) * x[2] + x[4] * x[3];
            return result;
        }
        public static double eq2(double[] x, double Omega)
        {
            return -x[6] * x[0] + x[4] * x[2] + (x[5] + Omega) * x[3];
        }
        public static double eq3(double[] x, double Omega)
        {
            return (x[5] - Omega) * x[0] - x[4] * x[2] + x[6] * x[3];
        }
        public static double eq4(double[] x, double Omega)
        {
            return -x[4] * x[0] + (-x[5] - Omega) * x[1] - x[6] * x[2];
        }
        public static double eq5(double[] x, double[] J, int[] u, double[] Mg)
        {
            return (-(J[2] - J[1]) * x[5] * x[6] + u[0] + Mg[0]) / J[0];
        }
        public static double eq6(double[] x, double[] J, int[] u_method, double[] Mg)
        {
            return (-(J[0] - J[2]) * x[4] * x[6] + u_method[1] + Mg[1]) / J[1];
        }
        public static double eq7(double[] x, double[] J, int[] u_method, double[] Mg)
        {
            return (-(J[1] - J[0]) * x[4] * x[5] + u_method[2] + Mg[2]) / J[2];
        }
    }

    class pPointEquations
    {
        public static double eq1(double[] x, double Omega, double[] p, double mu, double[] J, double R)
        {
            double threeMudivRo = (3 * mu) / (Math.Pow(R, 3));

            return -(-p[1] * x[6] + p[2] * (x[5] - Omega) - p[3] * x[4] +

                p[4] * (threeMudivRo *
                (-J[1] + J[2]) * (-2 * x[3] * ((x[3] * x[3] + x[2] * x[2]) - 1))) +

                p[5] * (threeMudivRo *
                (J[0] - J[2]) * (2 * x[2] * ((x[3] * x[3] + x[2] * x[2]) - 1))) +

                p[6] * (threeMudivRo *
                (-J[0] + J[1]) * ((-2 * x[3] * (2 * (x[0] * x[2] + x[3] * x[1]))) +
                ((2 * (x[1] * x[2] - x[3] * x[0])) * (2 * x[2])))));
        }
        public static double eq2(double[] x, double Omega, double[] p, double mu, double[] J, double R)
        {
            double threeMudivRo = (3 * mu) / (Math.Pow(R, 3));


            return -(p[0] * x[6] + p[3] * (-x[5] - Omega) +

                p[4] * (threeMudivRo *
                (-J[1] + J[2]) * (-2 * x[2] * ((x[3] * x[3] + x[2] * x[2]) - 1))) +

                p[5] * (threeMudivRo *
                (J[0] - J[2]) * (2 * x[3] * ((x[3] * x[3] + x[2] * x[2]) - 1))) +

                p[6] * (threeMudivRo *
                (-J[0] + J[1]) * ((-2 * x[2] * (2 * (x[0] * x[2] + x[3] * x[1]))) +
                ((2 * (x[1] * x[2] - x[3] * x[0])) * (2 * x[3])))));
        }

        public static double eq3(double[] x, double Omega, double[] p, double mu, double[] J, double R)
        {
            double threeMudivRo = (3 * mu) / (Math.Pow(R, 3));


            return -(p[1] * x[4] - p[2] * p[4] - p[3] * p[6] + p[0] * (-x[5] + Omega) +
                p[4] * (threeMudivRo *
                (-J[1] + J[2]) * ((2 * x[1] * ((x[3] * x[3] + x[2] * x[2]) - 1)) +
                ((2 * (x[1] * x[2] - x[3] * x[0])) * (2 * x[2] - 1)))) +
                p[5] * (threeMudivRo *
                (J[0] - J[2]) * ((2 * x[0] * ((x[3] * x[3] + x[2] * x[2]) - 1)) +
                ((2 * (x[0] * x[2] - x[3] * x[1])) * (2 * x[2] - 1)))) +
                p[6] * (threeMudivRo *
                (-J[0] + J[1]) * ((2 * x[1] * (2 * (x[0] * x[2] + x[3] * x[1]))) +
                ((2 * (x[1] * x[2] - x[3] * x[0])) * (2 * x[0])))));
        }


        public static double eq4(double[] x, double Omega, double[] p, double mu, double[] J, double R)
        {
            double threeMudivRo = (3 * mu) / (Math.Pow(R, 3));


            return -(p[0] * x[4] + p[2] * p[6] + p[1] * (-x[5] + Omega) +

                p[4] * (threeMudivRo *
                (-J[1] + J[2]) * ((2 * x[0] * ((x[3] * x[3] + x[2] * x[2]) - 1)) +
                ((2 * (x[1] * x[2] - x[3] * x[0])) * (2 * x[3] - 1)))) +

                p[5] * (threeMudivRo *
                (J[0] - J[2]) * ((2 * x[1] * ((x[3] * x[3] + x[2] * x[2]) - 1)) +
                ((2 * (x[0] * x[2] - x[3] * x[1])) * (2 * x[3] - 1)))) +

                p[6] * (threeMudivRo *
                (-J[0] + J[1]) * ((2 * x[1] * (2 * (x[0] * x[2] + x[3] * x[1]))) +
                ((2 * (x[1] * x[2] - x[3] * x[0])) * (2 * x[3])))));
        }


        public static double eq5(double[] x, double Omega, double[] p, double mu, double[] J, double R)
        {
            return -(p[0] * x[3] + p[1] * x[2] - p[3] * x[0] - p[2] * x[2] +
                p[5] * ((-(J[0] - J[2]) * x[6]) / J[1]) +
                p[6] * ((-(J[1] - J[0]) * x[5]) / J[2])
                );
        }

        public static double eq6(double[] x, double Omega, double[] p, double mu, double[] J, double R)
        {
            return -(p[0] * x[2] + p[1] * x[3] + p[2] * x[0] - p[3] * x[1] +
                 p[4] * ((-(J[2] - J[1]) * x[6]) / J[0]) +
                 p[6] * ((-(J[1] - J[0]) * x[5]) / J[2])
                 );
        }

        public static double eq7(double[] x, double Omega, double[] p, double mu, double[] J, double R)
        {
            return -(p[0] * x[1] - p[1] * x[0] + p[2] * x[3] - p[3] * x[2] +
                p[4] * ((-(J[2] - J[1]) * x[5]) / J[0]) +
                p[5] * ((-(J[0] - J[2]) * x[4]) / J[1])
                );
        }
    }


    //class Hestimation
    //{
    //    public static double H(double[] u, double[] p, double[] x, double Omega, double[] J, double[] Mg)
    //    {
    //        double sum = 0;
    //        for (int i = 0; i < x.Length; i++)
    //        {
    //            sum +=
    //                (p[0] * (x[6] * x[1] + (-x[5] + Omega) * x[2] + x[4] * x[3])) +
    //                (p[1] * (-x[6] * x[0] + x[4] * x[2] + (x[5] + Omega) * x[3])) +
    //                (p[2] * ((x[5] - Omega) * x[0] - x[4] * x[2] + x[6] * x[3])) +
    //                (p[3] * (-x[4] * x[0] + (-x[5] - Omega) * x[1] - x[6] * x[2])) +
    //                (p[4] * ((-(J[2] - J[1]) * x[5] * x[6] + u[0] + Mg[0]) / J[0])) +
    //                (p[5] * ((-(J[0] - J[2]) * x[4] * x[6] + u[1] + Mg[1]) / J[1])) +
    //                (p[6] * ((-(J[1] - J[0]) * x[4] * x[5] + u[2] + Mg[2]) / J[2]));
    //        }
    //        return sum;
    //    }
    //}


    class Convert_
    {
        public static double toDouble(object value)
        {
            NumberFormatInfo nfi = new NumberFormatInfo();
            nfi.NumberDecimalSeparator = ".";
            return double.Parse(value.ToString().Replace(',', '.'), nfi);
        }
    }
}
