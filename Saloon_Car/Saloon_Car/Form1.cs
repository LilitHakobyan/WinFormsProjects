using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Transactions;

namespace Saloon_Car
{
    public partial class Form1 : Form
    {
        private List<DataViewModel> dataViewModels;
        private int i;
        string DataPath = string.Empty;
        Saloon saloon = new Saloon("Avto.am");
        List<Brand> brendList = new List<Brand>();
        private string userName;
        private bool Role;
        public Form1()
        {
            GetUser();
            InitializeComponent();
            Vizible();
            Initializer();

        }

        public void GetUser()
        {
            LogInPage formLogIn = new LogInPage();
            formLogIn.ShowDialog();
            if (formLogIn.DialogResult == DialogResult.OK)
            {
                userName = formLogIn.Name;
                Role = formLogIn.Role;
            }
            else
            {
                //InitializeComponent();
                // Vizible();
                //Initializer();
                // this.Close();
                Application.Exit();
            }
        }
        public void Vizible()
        {
            if (Role == false)
            {
                this.button1.Visible = false;
                this.button2.Visible = false;
                this.button3.Visible = false;
            }
            else
            {
                this.button4.Visible = false;
                this.button5.Visible = false;
            }
        }
        public void Initializer()
        {
            dataViewModels = new List<DataViewModel>();
            using (TransactionScope scope = new TransactionScope())
            {
                using (SqlConnection connection = new SqlConnection(Utility.ConnectionString))
                {
                    connection.Open();

                    string sqlCommand = @"SELECT Car.CarID, Car.Sold,Car.Deleted,Car.Price,Model.ModelID,Model.ModelName,Model.Color, Brand.BrandID,Brand.BrandName
                FROM((Car
                INNER JOIN Model ON Car.ModelID = Model.ModelID)
                INNER JOIN Brand ON Model.BrandID = Brand.BrandID)";
                    SqlCommand command = new SqlCommand(sqlCommand, connection);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Brand brand = new Brand();
                            foreach (Brand item in brendList)
                            {
                                if (item.Name == reader["BrandName"].ToString())
                                    brand = item;
                            }
                            if (string.IsNullOrEmpty(brand.Name))
                            {
                                brand = new Brand
                                {
                                    Id = (int)reader["BrandID"],
                                    Name = reader["BrandName"].ToString(),
                                    models = new List<Model>()
                                };
                                brendList.Add(brand);
                            }

                            Model model = new Model()
                            {
                                Id = (int)reader["ModelID"],
                                Name = reader["ModelName"].ToString(),
                                Color = reader["Color"].ToString(),
                                Brand = brand,
                                BrandID = brand.Id

                            };
                            brand.models.Add(model);
                            Car car = new Car(model, (decimal)reader["Price"])
                            {
                                Deleted = (bool)reader["Deleted"],
                                Sold = (bool)reader["Sold"],
                                Id = (int)reader["CarID"],
                                ModelID = model.Id,
                                SalonID = 5
                            };

                            saloon.cars.Add(car);
                            dataViewModels.Add(new DataViewModel { Id = i++, Brand = brand.Name, Model = model.Name, Color = model.Color, Price = car.Price });
                        }
                    }
                }
                scope.Complete();
            }
            dataGridView1.DataSource = dataViewModels;
            i = dataViewModels.Last().Id;
        }
        private void buttonADD_Click(object sender, EventArgs e)
        {

            FormAddChangeCar formAdd = new FormAddChangeCar();
            formAdd.ShowDialog();
            if (formAdd.DialogResult == DialogResult.OK)
            {
                CreateNewItem(formAdd.BrandName, formAdd.ModelName, formAdd.ModelColor, formAdd.Price);
                dataViewModels.Add(new DataViewModel { Id = ++i, Brand = formAdd.BrandName, Model = formAdd.ModelName, Color = formAdd.ModelColor, Price = formAdd.Price, Sold = false });//
                RerefreshGridAndData();
            }

        }

        private void CreateNewItem(string brandName, string modelName, string modelColor, decimal price)
        {

            using (TransactionScope scope = new TransactionScope())
            {
                using (SqlConnection connection = new SqlConnection(Utility.ConnectionString))
                {
                    connection.Open();

                    Brand brand = new Brand();
                    foreach (Brand item in brendList)
                    {
                        if (item.Name == brandName)
                            brand = item;
                    }
                    if (string.IsNullOrEmpty(brand.Name))
                    {
                        brand = new Brand(brandName);
                        brendList.Add(brand);

                        string insertStr = "Insert into Brand([BrandName])";
                        insertStr += " Values(@Name)";
                        SqlCommand command = new SqlCommand(insertStr, connection);
                        command.Parameters.AddWithValue("Name", brandName);
                        command.ExecuteNonQuery();

                    }

                    string str = $"Select BrandID from Brand Where BrandName='{brandName}'";

                    SqlCommand findBrandId = new SqlCommand(str, connection);

                    SqlDataReader reader = findBrandId.ExecuteReader();

                    if (reader.HasRows)
                    {
                        reader.Read();
                        brand.Id = (int)reader["BrandID"];
                    }
                    reader.Close();


                    Model model = new Model()
                    {
                        Name = modelName,
                        Color = modelColor,
                        Brand = brand,
                        BrandID = brand.Id
                    };
                    brand.models.Add(model);

                    string insertStrModel = "Insert into Model([ModelName],[Color],BrandID)";
                    insertStrModel += " Values(@Name,@Color,@ID)";
                    SqlCommand commandModel = new SqlCommand(insertStrModel, connection);
                    commandModel.Parameters.AddWithValue("Name", modelName);
                    commandModel.Parameters.AddWithValue("Color", modelColor);
                    commandModel.Parameters.AddWithValue("ID", brand.Id);

                    commandModel.ExecuteNonQuery();


                    string strCar = $"Select ModelID from Model Where ModelName='{modelName}'";

                    SqlCommand finModelId = new SqlCommand(strCar, connection);

                    SqlDataReader readerM = finModelId.ExecuteReader();

                    if (readerM.HasRows)
                    {
                        readerM.Read();
                        model.Id = (int)readerM["ModelID"];
                    }
                    readerM.Close();

                    Car car = new Car(model, price);
                    car.ModelID = model.Id;
                    saloon.cars.Add(car);


                    string insertStrCar = "Insert into Car(Price,Sold,Deleted,ModelID)";
                    insertStrCar += " Values(@Price,@Sold,@Deleted,@ModelID)";
                    SqlCommand commandCar = new SqlCommand(insertStrCar, connection);
                    commandCar.Parameters.AddWithValue("Price", price);
                    commandCar.Parameters.AddWithValue("ModelID", model.Id);
                    commandCar.Parameters.AddWithValue("Sold", false);
                    commandCar.Parameters.AddWithValue("Deleted", false);

                    commandCar.ExecuteNonQuery();
                    //dataViewModels.Add(new DataViewModel { Id = i++, Brand = brand.Name, Model = model.Name, Color = model.Color, Price = car.Price });
                }
                scope.Complete();
            }
        }
        private void Edit_Click(object sender, EventArgs e)
        {
            try
            {
                DataViewModel viewModels = (DataViewModel)dataGridView1.SelectedRows[0]?.DataBoundItem;
                DataViewModel carItem = dataViewModels.FirstOrDefault(x => x.Id == viewModels.Id);
                FormAddChangeCar formAdd =
                    new FormAddChangeCar(carItem.Brand, carItem.Model, carItem.Color, carItem.Price);
                formAdd.ShowDialog();
                if (formAdd.DialogResult == DialogResult.OK)
                {
                    Car findCar = FindCarItem(carItem.Brand, carItem.Model, carItem.Color, carItem.Price);

                    if (findCar == null)
                    {
                        MessageBox.Show("Car not found");
                    }
                    else
                    {
                        carItem.Brand = formAdd.BrandName;
                        carItem.Model = formAdd.ModelName;
                        carItem.Color = formAdd.ModelColor;
                        carItem.Price = formAdd.Price;

                        findCar.Model.Brand.Name = formAdd.BrandName;
                        findCar.Model.Name = formAdd.ModelName;
                        findCar.Model.Color = formAdd.ModelColor;
                        findCar.Price = carItem.Price;
                        RerefreshGridAndData();
                    }
                }
            }

            catch (Exception)
            {
                MessageBox.Show(@"Please Choose a line");
            }

        }

        private void Delete_Click(object sender, EventArgs e)
        {
            try
            {
                DataViewModel viewModels = (DataViewModel)dataGridView1.SelectedRows[0]?.DataBoundItem;
                DataViewModel carItem = dataViewModels.FirstOrDefault(x => x.Id == viewModels.Id);
                carItem.Deleted = true;

                Car findCar = FindCarItem(carItem.Brand, carItem.Model, carItem.Color, carItem.Price);

                if (findCar == null)
                {
                    MessageBox.Show(@"Car not found");
                }
                else
                {
                    findCar.Deleted = true;
                    RerefreshGridAndData();
                }
            }
            catch (Exception)
            {
                MessageBox.Show(@"Please Choose a line");
            }

        }
        private void Buy_Click(object sender, EventArgs e)
        {
            try
            {
                DataViewModel viewModels = (DataViewModel)dataGridView1.SelectedRows[0]?.DataBoundItem;
                DataViewModel carItem = dataViewModels.FirstOrDefault(x => x.Id == viewModels.Id);
                if (carItem.Deleted == true)
                {
                    MessageBox.Show(@"Car id deleted");
                }
                else
                {
                    if (carItem.Sold == true)
                    {
                        MessageBox.Show(@"Car id sold");
                    }
                    else
                    {
                        carItem.Sold = true;

                        Car findCar = FindCarItem(carItem.Brand, carItem.Model, carItem.Color, carItem.Price);

                        if (findCar == null)
                        {
                            MessageBox.Show(@"Car not found");
                        }
                        else
                        {
                            findCar.Sold = true;
                            RerefreshGridAndData();
                        }
                    }
                }

            }
            catch (Exception)
            {
                MessageBox.Show(@"Please Choose a line");
            }

        }


        public void RerefreshGridAndData()
        {
            dataGridView1.DataSource = null;
            dataGridView1.DataSource = dataViewModels;

            //string serializeString = JsonConvert.SerializeObject(dataViewModels, Formatting.Indented,
            //    new JsonSerializerSettings
            //    {
            //        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            //    });
            //File.WriteAllText(DataPath, serializeString);

        }

        private void Close_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Search_Click(object sender, EventArgs e)
        {
            FormAddChangeCar formAdd = new FormAddChangeCar(true);
            formAdd.ShowDialog();
            if (formAdd.DialogResult == DialogResult.OK)
            {
                Car findCar = FindCarItem(formAdd.BrandName, formAdd.ModelName, formAdd.ModelColor);
                if (findCar == null)
                {
                    MessageBox.Show(@"Car not found");
                }
                else
                {
                    var rowId = dataViewModels.FirstOrDefault(item =>
                        item.Brand == findCar.Model.Brand.Name && item.Model == findCar.Model.Name &&
                        item.Color == findCar.Model.Color).Id;
                    dataGridView1.Rows[rowId - 1].Selected = true;

                }
            }

        }

        private Car FindCarItem(string brandName, string modelName, string modelColor,
            decimal price = decimal.Zero)
        {
            if (price == decimal.Zero)
            {
                Car resultCar = saloon.cars.FirstOrDefault(item =>
                    item.Model.Brand.Name.ToLower() == brandName.ToLower() &&
                    item.Model.Name.ToLower() == modelName.ToLower() &&
                    item.Model.Color.ToLower() == modelColor.ToLower()
                );
                return resultCar;
            }
            else
            {
                Car resultCar = saloon.cars.FirstOrDefault(item =>
                    item.Model.Brand.Name == brandName &&
                    item.Model.Name == modelName &&
                    item.Model.Color == modelColor &&
                    item.Price == price
                );
                return resultCar;
            }
        }

        private void SignOut_Click(object sender, EventArgs e)
        {

            Application.Restart();

        }
    }
}
