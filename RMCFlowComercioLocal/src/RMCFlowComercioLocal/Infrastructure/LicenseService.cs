using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;

namespace RMCFlowComercioLocal.Infrastructure
{
    internal static class LicenseService
    {
        public static string CreateRequestCode(string companyDocument)
        {
            var source = Environment.MachineName + "|" + Environment.UserDomainName + "|" + Environment.OSVersion.Version + "|" + (companyDocument ?? string.Empty).Trim();
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
                return "RMC-" + BitConverter.ToString(hash).Replace("-", string.Empty).Substring(0, 24);
            }
        }

        public static bool TryValidateSignedLicense(string signedLicense, string expectedRequestCode, string publicKeyPath, out string message)
        {
            message = null;
            try
            {
                var parts = (signedLicense ?? string.Empty).Trim().Split('.');
                if (parts.Length != 2) { message = "O bloco de licença está incompleto."; return false; }
                var payloadBytes = FromBase64Url(parts[0]);
                var signature = FromBase64Url(parts[1]);
                var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                if (!File.Exists(publicKeyPath)) { message = "A chave pública de validação não foi encontrada no aplicativo."; return false; }
                AsymmetricKeyParameter key;
                using (var reader = new StreamReader(publicKeyPath)) key = (AsymmetricKeyParameter)new PemReader(reader).ReadObject();
                var verifier = new RsaDigestSigner(new Org.BouncyCastle.Crypto.Digests.Sha256Digest());
                verifier.Init(false, (RsaKeyParameters)key);
                verifier.BlockUpdate(payloadBytes, 0, payloadBytes.Length);
                if (!verifier.VerifySignature(signature)) { message = "A assinatura da licença é inválida."; return false; }
                var values = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(payloadJson);
                if (!values.ContainsKey("product") || Convert.ToString(values["product"]) != "RMC Flow Comércio Local") { message = "Esta licença não pertence ao RMC Flow Comércio Local."; return false; }
                if (!values.ContainsKey("request_code") || !string.Equals(Convert.ToString(values["request_code"]), expectedRequestCode, StringComparison.Ordinal)) { message = "Esta licença pertence a outro computador."; return false; }
                if (!values.ContainsKey("type") || Convert.ToString(values["type"]) != "local_perpetual") { message = "O tipo de licença não é compatível."; return false; }
                message = Convert.ToString(values["license_code"]);
                return true;
            }
            catch (Exception ex) { message = "Não foi possível validar a licença: " + ex.Message; return false; }
        }

        public static bool TryValidatePasswordRecovery(string authorization,string expectedLicenseCode,string expectedRequestCode,string expectedNonce,string publicKeyPath,out string message)
        {
            message=null;
            try
            {
                var parts=(authorization??string.Empty).Trim().Split('.'); if(parts.Length!=2){message="A autorização de recuperação está incompleta.";return false;}
                var payloadBytes=FromBase64Url(parts[0]); var signature=FromBase64Url(parts[1]);
                if(!File.Exists(publicKeyPath)){message="A chave pública de validação não foi encontrada no aplicativo.";return false;}
                AsymmetricKeyParameter key; using(var reader=new StreamReader(publicKeyPath))key=(AsymmetricKeyParameter)new PemReader(reader).ReadObject();
                var verifier=new RsaDigestSigner(new Org.BouncyCastle.Crypto.Digests.Sha256Digest()); verifier.Init(false,(RsaKeyParameters)key); verifier.BlockUpdate(payloadBytes,0,payloadBytes.Length);
                if(!verifier.VerifySignature(signature)){message="A assinatura da autorização de recuperação é inválida.";return false;}
                var values=new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(Encoding.UTF8.GetString(payloadBytes));
                if(!values.ContainsKey("product")||Convert.ToString(values["product"])!="RMC Flow Comércio Local"){message="A autorização não pertence ao RMC Flow Comércio Local.";return false;}
                if(!values.ContainsKey("purpose")||Convert.ToString(values["purpose"])!="admin_password_reset"){message="Esta autorização não permite redefinir senha.";return false;}
                if(!values.ContainsKey("license_code")||!string.Equals(Convert.ToString(values["license_code"]),expectedLicenseCode,StringComparison.OrdinalIgnoreCase)){message="A autorização pertence a outra licença.";return false;}
                if(!values.ContainsKey("request_code")||!string.Equals(Convert.ToString(values["request_code"]),expectedRequestCode,StringComparison.Ordinal)){message="A autorização pertence a outro computador.";return false;}
                if(!values.ContainsKey("recovery_nonce")||!string.Equals(Convert.ToString(values["recovery_nonce"]),expectedNonce,StringComparison.Ordinal)){message="A solicitação de recuperação não confere ou já foi utilizada.";return false;}
                DateTime expires; if(!values.ContainsKey("expires_at")||!DateTime.TryParse(Convert.ToString(values["expires_at"]),null,System.Globalization.DateTimeStyles.AdjustToUniversal,out expires)||expires.ToUniversalTime()<=DateTime.UtcNow){message="A autorização de recuperação expirou.";return false;}
                message="Autorização validada.";return true;
            }
            catch(Exception ex){message="Não foi possível validar a autorização: "+ex.Message;return false;}
        }

        private static byte[] FromBase64Url(string value)
        {
            value = value.Replace('-', '+').Replace('_', '/');
            switch (value.Length % 4) { case 2: value += "=="; break; case 3: value += "="; break; }
            return Convert.FromBase64String(value);
        }
    }
}
