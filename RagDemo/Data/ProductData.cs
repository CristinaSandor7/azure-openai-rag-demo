using RagDemo.Models;

namespace RagDemo.Data
{
    public static class ProductData
    {
        public static List<Product> All = new()
        {
            new Product("Nitrile gloves", "Disposable gloves, chemical resistant, used in medical procedures and lab work. Available in sizes S, M, L."),
            new Product("Digital infrared thermometer", "Non-contact thermometer, measures body temperature in 2 seconds, suitable for clinics and home use."),
            new Product("Disposable syringes", "Sterile syringes 5ml and 10ml, individually packaged, for injections and sample collection."),
            new Product("Surgical masks 3-layer", "Respiratory protection masks, CE certified, box of 50 units."),
            new Product("Alcohol-based hand sanitizer", "Disinfectant solution 70% alcohol, 500ml bottle with pump dispenser."),
            new Product("Electronic arm blood pressure monitor", "Device for measuring blood pressure, digital screen, memory for 2 users.")
        };
    }
}
