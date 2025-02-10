import React from "react";

interface Location {
  Address1: string;
  City: string;
  State: string;
  Country: string;
  Zip: string;
  IsBilling: boolean;
}

interface Customer {
  Name: string;
  CustomerNumber: string;
  Phone: string;
  Website: string;
  Locations: Location[];
}

interface CustomerResponse {
  Data?: Customer[];
}

const SAMPLE: CustomerResponse = {
  Data: [
    {
      Name: "John Doe Corp",
      CustomerNumber: "CUST12345",
      Phone: "123-456-7890",
      Website: "https://johndoecorp.com",
      Locations: [
        {
          Address1: "123 Main St",
          City: "Los Angeles",
          State: "CA",
          Country: "USA",
          Zip: "90001",
          IsBilling: true,
        },
        {
          Address1: "456 Elm St",
          City: "San Francisco",
          State: "CA",
          Country: "USA",
          Zip: "94102",
          IsBilling: false,
        },
      ],
    },
  ],
};

const CustomerInfo: React.FC<{ response?: CustomerResponse }> = ({ response = SAMPLE}) => {
  if (!response.Data || response.Data.length === 0) return <p>No customer data available.</p>;
  const qaUrl = process.env.WESUITE_API_URL;
  return (
    <div className="p-4 border rounded-lg shadow-md">
      {response.Data.map((customer, custIndex) => (
        <div key={custIndex} className="mb-6 p-4 border rounded-md">
          <h2 className="text-xl font-bold">{customer.Name}</h2>
          <p className="text-gray-700">Customer Number: {customer.CustomerNumber}</p>
          <p className="text-gray-700">Phone: {customer.Phone}</p>
          <p className="text-gray-700">Website: <a href={customer.Website} className="text-blue-500">{customer.Website}</a></p>
          <h3 className="mt-4 text-lg font-semibold">Locations</h3>
          <ul>
            {customer.Locations.map((location, locIndex) => (
              <li key={locIndex} className="border-b py-2">
                <p>{location.Address1}, {location.City}, {location.State}, {location.Country} {location.Zip}</p>
                <p className="text-sm text-gray-500">{location.IsBilling ? "Billing Address" : "Service Address"}</p>
              </li>
            ))}
          </ul>
          <button 
            className="mt-4 px-4 py-2 bg-blue-500 text-white rounded hover:bg-blue-700"
            onClick={() => window.open(`${qaUrl}/Estimate/Customer/182252`, "_blank")}
          >
            Open in QuoteAnywhere
          </button>
        </div>
      ))}
    </div>
  );
};

export default CustomerInfo;