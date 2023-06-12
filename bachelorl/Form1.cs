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
        public double miu = 3.9660 * Math.Pow(10, 1);
        public double Ro = 6371000 + 35000000;

        public int[][] u = new int[8][];

        public Form1()
        {
            InitializeComponent();


            q_startGrid.Rows.Add(1, 0.04);
            q_startGrid.Rows.Add(2, 0.49);
            q_startGrid.Rows.Add(3, 0.36);
            q_startGrid.Rows.Add(4, 0.11);


            q_endGrid.Rows.Add(1, 0.25);
            q_endGrid.Rows.Add(2, 0.01);
            q_endGrid.Rows.Add(3, 0.16);
            q_endGrid.Rows.Add(4, 0.58);

            for (int i = 0; i < ws.Length; i++)
            {
                w_startGrid.Rows.Add(i + 1, (i + 1) * 0.001);
            }
            for (int i = 0; i < we.Length; i++)
            {
                w_endGrid.Rows.Add(i + 1, (i + 1) * 0.01);
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
            inertionDgv.Rows.Add(2, 165);
            inertionDgv.Rows.Add(3, 175);


            for (int i = 0; i < 7; i++)
            {
                pGrid.Rows.Add(i + 1, 0.01);
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
            dataGridView1.Rows.Clear();
            double epsilon = Convert_.toDouble(textBox1.Text);



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


            chart2.Series[0].Points.Clear();
            chart3.Series[0].Points.Clear();
            chart3.Series[1].Points.Clear();
            chart3.Series[2].Points.Clear();


            double AllTime = Convert_.toDouble(timeBox.Text);
            int N = Convert.ToInt32(Nbox.Text);

            double step = (double)AllTime / (double)N;




            List<double[]> x_on_last_iter = new List<double[]>();
            List<double[]> p_on_last_iter = new List<double[]>();

            //List<double[]> xHistory = new List<double[]>();
            //List<double[]> pHistory = new List<double[]>();
            //List<int> bestiters = new List<int>();


            double[] Y = new double[8];
            double[] Yold = new double[8];


            Y = new double[] { p[0], p[1], p[2], p[3], p[4], p[5], p[6], AllTime };
            Yold = new double[] { p[0], p[1], p[2], p[3], p[4], p[5], p[6], AllTime };
            Random R = new Random();
            double gamma = -1;
            double divider_delta = 0.001;


            double SUPERNEVIAZKA=0;
            bool nevizkaLock = false;
            int nevicount = 0;

            xgridOutput.Rows.Clear();
            pgridOutput.Rows.Clear();



            int maxSTOPiterations = 0;
            while (true)
            {
                maxSTOPiterations++;
                
                double HUmax = -100000000000;
                int bestUontheLastIteration = -1;

                for (int i = 0; i < 7; i++)
                {
                    double tmpSum = 0;
                    tmpSum += Y[0] * system1Equations.eq1(xs, Omega);
                    tmpSum += Y[1] * system1Equations.eq2(xs, Omega);
                    tmpSum += Y[2] * system1Equations.eq3(xs, Omega);
                    tmpSum += Y[3] * system1Equations.eq4(xs, Omega);


                    tmpSum += Y[4] * system1Equations.eq5(xs, J, u[i], Mg);
                    tmpSum += Y[5] * system1Equations.eq6(xs, J, u[i], Mg);
                    tmpSum += Y[6] * system1Equations.eq7(xs, J, u[i], Mg);

                    if (tmpSum > HUmax)
                    {
                        HUmax = tmpSum;
                        bestUontheLastIteration = i;
                    }
                }

                double[] xsN_t = new double[7] { xs[0], xs[1], xs[2], xs[3], xs[4], xs[5], xs[6] };
                double[] xsN_t_minus_odin = new double[7] { xs[0], xs[1], xs[2], xs[3], xs[4], xs[5], xs[6] };

                double[] p_t = new double[7] { Y[0], Y[1], Y[2], Y[3], Y[4], Y[5], Y[6] };
                double[] pt_minus_one = new double[7] { Y[0], Y[1], Y[2], Y[3], Y[4], Y[5], Y[6] };




                double currentTime = 0;
                int counter = 0;




                x_on_last_iter = new List<double[]>();
                p_on_last_iter = new List<double[]>();


                AllTime = Math.Abs( Y[7]);
                step = AllTime / (double)N;


                if(currentTime> AllTime)
                {
                    int zzzzzzzzzzz = 23;
                }


                while (currentTime < AllTime) // ДАЛІ - метод ЕЙЛЕРА (цей цикл)
                {
                    counter++;
                    double quadratXSum = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        quadratXSum += xsN_t_minus_odin[i] * xsN_t_minus_odin[i];
                    }
                    quadratXSum = Math.Sqrt(quadratXSum);

                    xsN_t[0] = (xsN_t_minus_odin[0] + step * system1Equations.eq1(xsN_t_minus_odin, Omega)) / quadratXSum;
                    xsN_t[1] = (xsN_t_minus_odin[1] + step * system1Equations.eq2(xsN_t_minus_odin, Omega)) / quadratXSum;
                    xsN_t[2] = (xsN_t_minus_odin[2] + step * system1Equations.eq3(xsN_t_minus_odin, Omega)) / quadratXSum;
                    xsN_t[3] = (xsN_t_minus_odin[3] + step * system1Equations.eq4(xsN_t_minus_odin, Omega)) / quadratXSum;
                    xsN_t[4] = xsN_t_minus_odin[4] + step * system1Equations.eq5(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);
                    xsN_t[5] = xsN_t_minus_odin[5] + step * system1Equations.eq6(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);
                    xsN_t[6] = xsN_t_minus_odin[6] + step * system1Equations.eq7(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);

                    double quadratPSum = 0;
                    for (int i = 0; i < 7; i++)
                    {
                        quadratPSum = pt_minus_one[i] * pt_minus_one[i];
                    }
                    quadratPSum = Math.Sqrt(quadratPSum);

                    p_t[0] = (pt_minus_one[0] + step * pPointEquations.eq1(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[1] = (pt_minus_one[1] + step * pPointEquations.eq2(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[2] = (pt_minus_one[2] + step * pPointEquations.eq3(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[3] = (pt_minus_one[3] + step * pPointEquations.eq4(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[4] = (pt_minus_one[4] + step * pPointEquations.eq5(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro)) ;
                    p_t[5] = (pt_minus_one[5] + step * pPointEquations.eq6(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro)) ;
                    p_t[6] = (pt_minus_one[6] + step * pPointEquations.eq7(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro)) ;


                    //тут ми виводимо наші дані в табличку і саме тут видноо що вони погані (безкінечність або НЕчисло)







                    //pgridOutput.Rows.Add(counter, p_t[0], p_t[1], p_t[2], p_t[3], p_t[4], p_t[5], p_t[6]);
                    //xgridOutput.Rows.Add(counter, xsN_t[0], xsN_t[1], xsN_t[2], xsN_t[3], xsN_t[4], xsN_t[5], xsN_t[6]);


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

                    currentTime += step;

                    xsN_t_minus_odin = new double[7] { xsN_t[0], xsN_t[1], xsN_t[2], xsN_t[3], xsN_t[4], xsN_t[5], xsN_t[6] };
                    pt_minus_one = new double[7] { p_t[0], p_t[1], p_t[2], p_t[3], p_t[4], p_t[5], p_t[6] };

                    x_on_last_iter.Add(new double[] { xsN_t[0], xsN_t[1], xsN_t[2], xsN_t[3], xsN_t[4], xsN_t[5], xsN_t[6] });
                    p_on_last_iter.Add(new double[] { p_t[0], p_t[1], p_t[2], p_t[3], p_t[4], p_t[5], p_t[6] });

                }


                
                double neviazka_bez_zburennia = 0;
                neviazka_bez_zburennia += p_t[0] * system1Equations.eq1(xsN_t, Omega);
                neviazka_bez_zburennia += p_t[1] * system1Equations.eq2(xsN_t, Omega);
                neviazka_bez_zburennia += p_t[2] * system1Equations.eq3(xsN_t, Omega);
                neviazka_bez_zburennia += p_t[3] * system1Equations.eq4(xsN_t, Omega);
                neviazka_bez_zburennia += p_t[4] * system1Equations.eq5(xsN_t, J, u[bestUontheLastIteration], Mg);
                neviazka_bez_zburennia += p_t[5] * system1Equations.eq6(xsN_t, J, u[bestUontheLastIteration], Mg);
                neviazka_bez_zburennia += p_t[6] * system1Equations.eq7(xsN_t, J, u[bestUontheLastIteration], Mg);
                neviazka_bez_zburennia = Math.Pow(neviazka_bez_zburennia - 1, 2);
                for (int j = 0; j < xe.Length; j++)
                {
                    neviazka_bez_zburennia += Math.Pow(xe[j] - xsN_t[j], 2);
                }
                if (!nevizkaLock)
                {
                    SUPERNEVIAZKA = neviazka_bez_zburennia;
                    dataGridView1.Rows.Add(nevicount, SUPERNEVIAZKA);
                    chart2.Series[0].Points.AddY(SUPERNEVIAZKA);
                }
                nevizkaLock = true;




                double[] neviazky_zi_zburenniam = new double[8];
                double delta = R.NextDouble();
                List<double> deltas = new List<double>();
                for (int p_neviazka_index = 0; p_neviazka_index < 7; p_neviazka_index++)
                {
                    delta = R.NextDouble() - 0.5;
                    deltas.Add(delta);
                    p_t = new double[7] { Y[0], Y[1], Y[2], Y[3], Y[4], Y[5], Y[6] };
                    pt_minus_one = new double[7] { Y[0], Y[1], Y[2], Y[3], Y[4], Y[5], Y[6] };
                    xsN_t = new double[7] { xs[0], xs[1], xs[2], xs[3], xs[4], xs[5], xs[6] };
                    xsN_t_minus_odin = new double[7] { xs[0], xs[1], xs[2], xs[3], xs[4], xs[5], xs[6] };

                    p_t[p_neviazka_index] += delta;
                    pt_minus_one[p_neviazka_index] += delta;
                    currentTime = 0;

                    while (currentTime < AllTime) // ДАЛІ - метод ЕЙЛЕРА (цей цикл)
                    {
                        double quadratXSum = 0;
                        for (int i = 0; i < 4; i++)
                        {
                            quadratXSum += xsN_t_minus_odin[i] * xsN_t_minus_odin[i];
                        }
                        quadratXSum = Math.Sqrt(quadratXSum);

                        xsN_t[0] = (xsN_t_minus_odin[0] + step * system1Equations.eq1(xsN_t_minus_odin, Omega)) / quadratXSum;
                        xsN_t[1] = (xsN_t_minus_odin[1] + step * system1Equations.eq2(xsN_t_minus_odin, Omega)) / quadratXSum;
                        xsN_t[2] = (xsN_t_minus_odin[2] + step * system1Equations.eq3(xsN_t_minus_odin, Omega)) / quadratXSum;
                        xsN_t[3] = (xsN_t_minus_odin[3] + step * system1Equations.eq4(xsN_t_minus_odin, Omega)) / quadratXSum;
                        xsN_t[4] = xsN_t_minus_odin[4] + step * system1Equations.eq5(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);
                        xsN_t[5] = xsN_t_minus_odin[5] + step * system1Equations.eq6(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);
                        xsN_t[6] = xsN_t_minus_odin[6] + step * system1Equations.eq7(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);

                        double quadratPSum = 0;
                        for (int i = 0; i < 7; i++)
                        {
                            quadratPSum += pt_minus_one[i] * pt_minus_one[i];
                        }
                        quadratPSum = Math.Sqrt(quadratPSum);


                        p_t[0] = (pt_minus_one[0] + step * pPointEquations.eq1(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                        p_t[1] = (pt_minus_one[1] + step * pPointEquations.eq2(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                        p_t[2] = (pt_minus_one[2] + step * pPointEquations.eq3(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                        p_t[3] = (pt_minus_one[3] + step * pPointEquations.eq4(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                        p_t[4] = (pt_minus_one[4] + step * pPointEquations.eq5(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                        p_t[5] = (pt_minus_one[5] + step * pPointEquations.eq6(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                        p_t[6] = (pt_minus_one[6] + step * pPointEquations.eq7(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
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
                        currentTime += step;
                        xsN_t_minus_odin = new double[7] { xsN_t[0], xsN_t[1], xsN_t[2], xsN_t[3], xsN_t[4], xsN_t[5], xsN_t[6] };
                        pt_minus_one = new double[7] { p_t[0], p_t[1], p_t[2], p_t[3], p_t[4], p_t[5], p_t[6] };
                    }


                    neviazky_zi_zburenniam[p_neviazka_index] = 0;
                    neviazky_zi_zburenniam[p_neviazka_index] += Y[0] * system1Equations.eq1(xsN_t, Omega);
                    neviazky_zi_zburenniam[p_neviazka_index] += Y[1] * system1Equations.eq2(xsN_t, Omega);
                    neviazky_zi_zburenniam[p_neviazka_index] += Y[2] * system1Equations.eq3(xsN_t, Omega);
                    neviazky_zi_zburenniam[p_neviazka_index] += Y[3] * system1Equations.eq4(xsN_t, Omega);
                    neviazky_zi_zburenniam[p_neviazka_index] += Y[4] * system1Equations.eq5(xsN_t, J, u[bestUontheLastIteration], Mg);
                    neviazky_zi_zburenniam[p_neviazka_index] += Y[5] * system1Equations.eq6(xsN_t, J, u[bestUontheLastIteration], Mg);
                    neviazky_zi_zburenniam[p_neviazka_index] += Y[6] * system1Equations.eq7(xsN_t, J, u[bestUontheLastIteration], Mg);
                    neviazky_zi_zburenniam[p_neviazka_index] = Math.Pow(neviazka_bez_zburennia - 1, 2);

                    for (int j = 0; j < xe.Length; j++)
                    {
                        neviazky_zi_zburenniam[p_neviazka_index] += Math.Pow(xe[j] - xsN_t[j], 2);
                    }
                }
                //тут у нас уже є нев'язки зі збуреннями по П



                delta = R.NextDouble()-0.5;
                deltas.Add(delta);
                p_t = new double[7] { Y[0], Y[1], Y[2], Y[3], Y[4], Y[5], Y[6] };
                pt_minus_one = new double[7] { Y[0], Y[1], Y[2], Y[3], Y[4], Y[5], Y[6] };
                xsN_t = new double[7] { xs[0], xs[1], xs[2], xs[3], xs[4], xs[5], xs[6] };
                xsN_t_minus_odin = new double[7] { xs[0], xs[1], xs[2], xs[3], xs[4], xs[5], xs[6] };
                currentTime = 0;

                while (currentTime < AllTime + delta) // ДАЛІ - метод ЕЙЛЕРА (цей цикл)
                {
                    double quadratXSum = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        quadratXSum += xsN_t_minus_odin[i] * xsN_t_minus_odin[i];
                    }
                    quadratXSum = Math.Sqrt(quadratXSum);

                    xsN_t[0] = (xsN_t_minus_odin[0] + step * system1Equations.eq1(xsN_t_minus_odin, Omega)) / quadratXSum;
                    xsN_t[1] = (xsN_t_minus_odin[1] + step * system1Equations.eq2(xsN_t_minus_odin, Omega)) / quadratXSum;
                    xsN_t[2] = (xsN_t_minus_odin[2] + step * system1Equations.eq3(xsN_t_minus_odin, Omega)) / quadratXSum;
                    xsN_t[3] = (xsN_t_minus_odin[3] + step * system1Equations.eq4(xsN_t_minus_odin, Omega)) / quadratXSum;
                    xsN_t[4] = xsN_t_minus_odin[4] + step * system1Equations.eq5(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);
                    xsN_t[5] = xsN_t_minus_odin[5] + step * system1Equations.eq6(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);
                    xsN_t[6] = xsN_t_minus_odin[6] + step * system1Equations.eq7(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);

                    double quadratPSum = 0;
                    for (int i = 0; i < 7; i++)
                    {
                        quadratPSum = pt_minus_one[i] * pt_minus_one[i];
                    }
                    quadratPSum = Math.Sqrt(quadratPSum);


                    p_t[0] = (pt_minus_one[0] + step * pPointEquations.eq1(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[1] = (pt_minus_one[1] + step * pPointEquations.eq2(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[2] = (pt_minus_one[2] + step * pPointEquations.eq3(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[3] = (pt_minus_one[3] + step * pPointEquations.eq4(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[4] = (pt_minus_one[4] + step * pPointEquations.eq5(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[5] = (pt_minus_one[5] + step * pPointEquations.eq6(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[6] = (pt_minus_one[6] + step * pPointEquations.eq7(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
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
                    currentTime += step;
                    xsN_t_minus_odin = new double[7] { xsN_t[0], xsN_t[1], xsN_t[2], xsN_t[3], xsN_t[4], xsN_t[5], xsN_t[6] };
                    pt_minus_one = new double[7] { p_t[0], p_t[1], p_t[2], p_t[3], p_t[4], p_t[5], p_t[6] };
                }
               

                neviazky_zi_zburenniam[7] = 0;
                neviazky_zi_zburenniam[7] += Y[0] * system1Equations.eq1(xsN_t, Omega);
                neviazky_zi_zburenniam[7] += Y[1] * system1Equations.eq2(xsN_t, Omega);
                neviazky_zi_zburenniam[7] += Y[2] * system1Equations.eq3(xsN_t, Omega);
                neviazky_zi_zburenniam[7] += Y[3] * system1Equations.eq4(xsN_t, Omega);
                neviazky_zi_zburenniam[7] += Y[4] * system1Equations.eq5(xsN_t, J, u[bestUontheLastIteration], Mg);
                neviazky_zi_zburenniam[7] += Y[5] * system1Equations.eq6(xsN_t, J, u[bestUontheLastIteration], Mg);
                neviazky_zi_zburenniam[7] += Y[6] * system1Equations.eq7(xsN_t, J, u[bestUontheLastIteration], Mg);
                neviazky_zi_zburenniam[7] = Math.Pow(neviazka_bez_zburennia - 1, 2);

                for (int j = 0; j < xe.Length; j++)
                {
                    neviazky_zi_zburenniam[7] += Math.Pow(xe[j] - xsN_t[j], 2);
                }
                //тут у нас уже є нев'язки зі збуреннями по П і по Т



                double[] gradient = new double[8];

                for (int grad_index = 0; grad_index < 8; grad_index++)
                {
                    gradient[grad_index] = (neviazky_zi_zburenniam[grad_index] - neviazka_bez_zburennia) / deltas[0];
                }


                double[] Y_tmp = new double[8];
                for (int i = 0; i < 8; i++)
                {
                    Y_tmp[i] = Y[i] - gradient[i] * gamma;
                }
                p_t = new double[7] { Y_tmp[0], Y_tmp[1], Y_tmp[2], Y_tmp[3], Y_tmp[4], Y_tmp[5], Y_tmp[6] };
                pt_minus_one = new double[7] { Y_tmp[0], Y_tmp[1], Y_tmp[2], Y_tmp[3], Y_tmp[4], Y_tmp[5], Y_tmp[6] };
                xsN_t = new double[7] { xs[0], xs[1], xs[2], xs[3], xs[4], xs[5], xs[6] };
                xsN_t_minus_odin = new double[7] { xs[0], xs[1], xs[2], xs[3], xs[4], xs[5], xs[6] };
                currentTime = 0;
                while (currentTime < AllTime) // ДАЛІ - метод ЕЙЛЕРА (цей цикл)
                {
                    counter++;
                    double quadratXSum = 0;
                    for (int i = 0; i < 4; i++)
                    {
                        quadratXSum += xsN_t_minus_odin[i] * xsN_t_minus_odin[i];
                    }
                    quadratXSum = Math.Sqrt(quadratXSum);

                    xsN_t[0] = (xsN_t_minus_odin[0] + step * system1Equations.eq1(xsN_t_minus_odin, Omega)) / quadratXSum;
                    xsN_t[1] = (xsN_t_minus_odin[1] + step * system1Equations.eq2(xsN_t_minus_odin, Omega)) / quadratXSum;
                    xsN_t[2] = (xsN_t_minus_odin[2] + step * system1Equations.eq3(xsN_t_minus_odin, Omega)) / quadratXSum;
                    xsN_t[3] = (xsN_t_minus_odin[3] + step * system1Equations.eq4(xsN_t_minus_odin, Omega)) / quadratXSum;
                    xsN_t[4] = xsN_t_minus_odin[4] + step * system1Equations.eq5(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);
                    xsN_t[5] = xsN_t_minus_odin[5] + step * system1Equations.eq6(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);
                    xsN_t[6] = xsN_t_minus_odin[6] + step * system1Equations.eq7(xsN_t_minus_odin, J, u[bestUontheLastIteration], Mg);

                    double quadratPSum = 0;
                    for (int i = 0; i < 7; i++)
                    {
                        quadratPSum = pt_minus_one[i] * pt_minus_one[i];
                    }
                    quadratPSum = Math.Sqrt(quadratPSum);


                    p_t[0] = (pt_minus_one[0] + step * pPointEquations.eq1(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[1] = (pt_minus_one[1] + step * pPointEquations.eq2(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[2] = (pt_minus_one[2] + step * pPointEquations.eq3(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[3] = (pt_minus_one[3] + step * pPointEquations.eq4(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[4] = (pt_minus_one[4] + step * pPointEquations.eq5(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[5] = (pt_minus_one[5] + step * pPointEquations.eq6(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));
                    p_t[6] = (pt_minus_one[6] + step * pPointEquations.eq7(xsN_t_minus_odin, Omega, pt_minus_one, miu, J, Ro));

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

                    currentTime += step;

                    xsN_t_minus_odin = new double[7] { xsN_t[0], xsN_t[1], xsN_t[2], xsN_t[3], xsN_t[4], xsN_t[5], xsN_t[6] };
                    pt_minus_one = new double[7] { p_t[0], p_t[1], p_t[2], p_t[3], p_t[4], p_t[5], p_t[6] };
                }



                double neviazka_bez_zburenniaNEW = 0;
                neviazka_bez_zburenniaNEW += p_t[0] * system1Equations.eq1(xsN_t, Omega);
                neviazka_bez_zburenniaNEW += p_t[1] * system1Equations.eq2(xsN_t, Omega);
                neviazka_bez_zburenniaNEW += p_t[2] * system1Equations.eq3(xsN_t, Omega);
                neviazka_bez_zburenniaNEW += p_t[3] * system1Equations.eq4(xsN_t, Omega);
                neviazka_bez_zburenniaNEW += p_t[4] * system1Equations.eq5(xsN_t, J, u[bestUontheLastIteration], Mg);
                neviazka_bez_zburenniaNEW += p_t[5] * system1Equations.eq6(xsN_t, J, u[bestUontheLastIteration], Mg);
                neviazka_bez_zburenniaNEW += p_t[6] * system1Equations.eq7(xsN_t, J, u[bestUontheLastIteration], Mg);
                neviazka_bez_zburenniaNEW = Math.Pow(neviazka_bez_zburenniaNEW - 1, 2);
                for (int j = 0; j < xe.Length; j++)
                {
                    neviazka_bez_zburenniaNEW += Math.Pow(xe[j] - xsN_t[j], 2);
                }

                
                if (neviazka_bez_zburenniaNEW < SUPERNEVIAZKA)
                {
                    
                    
                    Y = new double[] { Y_tmp[0], Y_tmp[1], Y_tmp[2], Y_tmp[3], Y_tmp[4], Y_tmp[5], Y_tmp[6], Y_tmp[7] };
                    gamma = -1;
                    nevicount++;
                    dataGridView1.Rows.Add(nevicount, neviazka_bez_zburenniaNEW);
                    chart2.Series[0].Points.AddY(neviazka_bez_zburenniaNEW);
                    chart3.Series[0].Points.AddXY(nevicount, u[bestUontheLastIteration][0]);
                    chart3.Series[1].Points.AddXY(nevicount, u[bestUontheLastIteration][1]);
                    chart3.Series[2].Points.AddXY(nevicount, u[bestUontheLastIteration][2]);
                    if (nevicount > 30 || Math.Abs(neviazka_bez_zburenniaNEW - SUPERNEVIAZKA)<epsilon)
                       break;
                    else
                        SUPERNEVIAZKA = neviazka_bez_zburenniaNEW;
                }
                else
                {
                    gamma /= 2;
                }


                if (maxSTOPiterations > 1000)
                    break;
            }
            xgridOutput.Rows.Clear();
            for (int seriesIndex = 0; seriesIndex < chart1.Series.Count; seriesIndex++)
            {
                chart1.Series[seriesIndex].Points.Clear();
                for (int i = 0; i < x_on_last_iter.Count; i++)
                {
                    chart1.Series[seriesIndex].Points.AddXY(i, x_on_last_iter[i][seriesIndex]);

                }
            }
            for (int i = 0; i < x_on_last_iter.Count; i++)
            {
                xgridOutput.Rows.Add(i, x_on_last_iter[i][0], x_on_last_iter[i][1], x_on_last_iter[i][2], x_on_last_iter[i][3], x_on_last_iter[i][4], x_on_last_iter[i][5], x_on_last_iter[i][6]);
                pgridOutput.Rows.Add(i, p_on_last_iter[i][0], p_on_last_iter[i][1], p_on_last_iter[i][2], p_on_last_iter[i][3], p_on_last_iter[i][4], p_on_last_iter[i][5], p_on_last_iter[i][6]);
            }
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
