using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace LessonsSamples.Lesson7.CohesionCoupling
{
    // Some of the function parameters were extracted as class fields

    public class PageXmlExport_1
    {
        private const string exportFolder = @"c:\temp";

        private readonly string fileNameFormat;     //used in 3/5 methods
        private readonly bool overwrite;            //used in 3/5 methods
        
        private readonly int maxSalesOrders;        // used in 3/5 methods
        private readonly bool addCustomerDetails;   // used in 2/5 methods
        
        public PageXmlExport_1(
            string fileNameFormat, 
            bool overwrite, 
            int maxSalesOrders, 
            bool addCustomerDetails)
        {
            this.fileNameFormat = fileNameFormat;
            this.overwrite = overwrite;
            this.maxSalesOrders = maxSalesOrders;
            this.addCustomerDetails = addCustomerDetails;
        }

        public bool ExportCustomerPage(string customerName)
        {
            string fileName = string.Format(fileNameFormat, "CustomerPage", customerName, DateTime.Now);
            string filePath = Path.Combine(exportFolder, fileName);

            if (!overwrite && File.Exists(filePath))
                return false;


            PageXml content = new PageXml { Customer = new CustomerXml { Name = customerName } };

            using (EfRepository repository = new EfRepository())
            {
                if (maxSalesOrders > 0)
                {
                    var orders = repository.GetEntities<Order>()
                                           .Where(o => o.Customer.CompanyName == content.Customer.Name)
                                           .OrderBy(o => o.OrderDate)
                                           .Take(maxSalesOrders);

                    //enrich content with orders
                    // ...
                }

                if (addCustomerDetails)
                {
                    var customer = repository.GetEntities<Customer>()
                                             .Where(c => c.CompanyName == customerName);

                    // enrich content with customer data
                    // ...
                }
            }


            XmlSerializer serializer = new XmlSerializer(typeof(PageXml));
            using (StreamWriter sw = File.CreateText(filePath))
            {
                serializer.Serialize(sw, content);
            }
            return true;
        }

        public bool ExportCustomerPageWithExternalData(
            string customerName, 
            PageData externalData, 
            ICrmService crmService, 
            ILocationService locationService)
        {
            string fileName = string.Format("{0}-{1}.xml", fileNameFormat, customerName);
            string filePath = Path.Combine(exportFolder, fileName);

            if (!overwrite && File.Exists(filePath))
                return false;


            PageXml content = new PageXml { Customer = new CustomerXml { Name = customerName } };

            if (externalData.CustomerData != null)
            {
                // enrich with externalData.CustomerData 
                // ...
            }
            else
            {
                CustomerInfo customerData = crmService.GetCustomerInfo(content.Customer.Name);
            }

            using (EfRepository repository = new EfRepository())
            {
                if (maxSalesOrders > 0)
                {
                    var orders = repository.GetEntities<Order>()
                                           .Where(o => o.Customer.CompanyName == content.Customer.Name)
                                           .OrderBy(o => o.OrderDate)
                                           .Take(maxSalesOrders);

                    //enrich content with orders
                    // ...
                }

                if (addCustomerDetails)
                {
                    var customer = repository.GetEntities<Customer>()
                                             .Where(c => c.CompanyName == customerName);

                    // enrich content by merging the external customer data with what read from DB
                    // ...
                }
            }

            if (locationService != null)
            {
                foreach (var address in content.Customer.Addresses)
                {
                    Coordinates coordinates = locationService.GetCoordinates(address.City, address.Street, address.Number);
                    if (coordinates != null)
                        address.Coordinates = string.Format("{0},{1}", coordinates.Latitude, coordinates.Longitude);
                }
            }


            XmlSerializer serializer = new XmlSerializer(typeof(PageXml));
            using (StreamWriter sw = File.CreateText(filePath))
            {
                serializer.Serialize(sw, content);
            }
            return true;
        }

        public bool ExportOrders(string customerName)
        {
            string fileName = string.Format(fileNameFormat, "CustomerOrdersPage", customerName, DateTime.Now);
            string filePath = Path.Combine(exportFolder, fileName);

            if (!overwrite && File.Exists(filePath))
                return false;

            PageXml content = new PageXml { Customer = new CustomerXml { Name = customerName } };

            using (EfRepository repository = new EfRepository())
            {
                var orders = repository.GetEntities<Order>()
                                       .Where(o => o.Customer.CompanyName == content.Customer.Name)
                                       .OrderBy(o => o.ApprovedAmmount)
                                       .ThenBy(o=>o.OrderDate);

                //enrich content with orders
            }

            XmlSerializer serializer = new XmlSerializer(typeof(PageXml));
            using (StreamWriter sw = File.CreateText(filePath))
            {
                serializer.Serialize(sw, content);
            }
            return true;
        }

        public IEnumerable<PageXml> GetPagesFromOrders( 
			IEnumerable<Order> orders,
			ICrmService crmService, 
            ILocationService locationService)
        {
            Dictionary<string, IEnumerable<Order>> customerOrders = GroupOrdersByCustomer(orders);
            foreach (var customerName in customerOrders.Keys)
            {
                PageXml content = new PageXml { Customer = new CustomerXml { Name = customerName } };

                if (crmService != null)
                {
                    CustomerInfo customerData = crmService.GetCustomerInfo(content.Customer.Name);
                    //enrich with data from crm
                }

                var recentOrders = customerOrders[customerName]
                    .OrderBy(o => o.OrderDate)
                    .Take(maxSalesOrders);
                foreach (var order in recentOrders)
                {
                    // enrich content with orders
                    // ...
                }

                if (locationService != null)
                {
                    foreach (var address in content.Customer.Addresses)
                    {
                        Coordinates coordinates = locationService.GetCoordinates(address.City, address.Street, address.Number);
                        if (coordinates != null)
                            address.Coordinates = string.Format("{0},{1}", coordinates.Latitude, coordinates.Longitude);
                    }
                }

                yield return content;
            }
        }

        private Dictionary<string, IEnumerable<Order>> GroupOrdersByCustomer(
			IEnumerable<Order> orders)
        {
            // group orders by customer name and return them in a dictionary, ordered by OrderDate
            throw new NotImplementedException();
        }

        public bool ExportPagesFromOrders(
			IEnumerable<Order> orders, 
            ICrmService crmService, 
            ILocationService locationService)
        {
            IEnumerable<PageXml> pages = GetPagesFromOrders(orders, crmService, locationService);
            foreach (var pageXml in pages)
            {
                string customerName = pageXml.Customer.Name;
                string fileName = string.Format("CustomerOrders-{0}-{1}.xml", customerName, DateTime.Now);
                string filePath = Path.Combine(exportFolder, fileName);

                if (!overwrite && File.Exists(filePath))
                    return false;

                XmlSerializer serializer = new XmlSerializer(typeof(PageXml));
                using (StreamWriter sw = File.CreateText(filePath))
                {
                    serializer.Serialize(sw, pageXml);
                }
            }
            return true;
        }
    }
}