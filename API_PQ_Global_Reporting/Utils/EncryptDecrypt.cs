namespace API_PQ_Global_Reporting.Utils
{
    public class EncryptDecrypt
    {
        public string DecryptString(string encrString)
        {
            byte[] b;
            byte[] b1;
            string decrypted;
            string decry;
            try
            {
                b = Convert.FromBase64String(encrString);
                decry = System.Text.ASCIIEncoding.ASCII.GetString(b);
                b1 = Convert.FromBase64String(decry);
                decrypted = System.Text.ASCIIEncoding.ASCII.GetString(b1);

            }
            catch (FormatException)
            {
                decrypted = "";
            }
            return decrypted;
        }

        public string EnryptString(string strEncrypted)
        {
            byte[] b = System.Text.ASCIIEncoding.ASCII.GetBytes(strEncrypted);
            string encry = Convert.ToBase64String(b);

            byte[] b1 = System.Text.ASCIIEncoding.ASCII.GetBytes(encry);
            string encrypted = Convert.ToBase64String(b1);
            return encrypted;
        }
    }
}