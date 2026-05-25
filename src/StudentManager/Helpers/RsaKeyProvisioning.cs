namespace StudentManager.Helpers
{
    public static class RsaKeyProvisioning
    {
        private const string NV01_Public = @"<RSAKeyValue><Modulus>tvuckc4UMfKmMyMXqpbLWIJ9XEQjr97ZJ6Z5n4XXGxVVsgb60GckvucFAvThW6RYs+mWG7/83EdIjJps5sowMh5S6uaofa2catwsLe/3jY9kkcFldu1RgOGPJwvRkMxs7J5jBMhdcvCDFQEj9Y+eodrNtT5QqHxKgZRfaOQDszOwD1YOYy0zcUfstRePjsyflkBN+O8dBg7B4460J1vXQD/JxEWiZFltbd67CVgpdA84V9pt+iXkTJV2HejTxRKtOn315dK1Q629nm+wTVf0fsh2g1VxEgnHqnT/6SbFNg3HnHRldFgmDyIJLSpTuiKmJfFZlQGo6cYM1pJSMB8WoQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        private const string NV01_Private = @"<RSAKeyValue><Modulus>tvuckc4UMfKmMyMXqpbLWIJ9XEQjr97ZJ6Z5n4XXGxVVsgb60GckvucFAvThW6RYs+mWG7/83EdIjJps5sowMh5S6uaofa2catwsLe/3jY9kkcFldu1RgOGPJwvRkMxs7J5jBMhdcvCDFQEj9Y+eodrNtT5QqHxKgZRfaOQDszOwD1YOYy0zcUfstRePjsyflkBN+O8dBg7B4460J1vXQD/JxEWiZFltbd67CVgpdA84V9pt+iXkTJV2HejTxRKtOn315dK1Q629nm+wTVf0fsh2g1VxEgnHqnT/6SbFNg3HnHRldFgmDyIJLSpTuiKmJfFZlQGo6cYM1pJSMB8WoQ==</Modulus><Exponent>AQAB</Exponent><P>yn3DXUiyG2yJptxBzXJVsR0zHfnvd4MlqSjkfu/jz7nTlwZ+FmI/vEKGW0T6oQYzB14l3kalbGt9ppL1WbSmPHnVo227Nka84jF2M3c+syWntJWeQ6nIIuRKNCxrKpX+00/BaeAj0sbq1ENjoXLN7yXSlQ3jeYvQVIuMyNzHRyM=</P><Q>51YiLeOuOus8sL8oaW6Lhncb4VP3rfanSS0xUIrHoEfBXVUglvOIljsEVzXlUygSHcf/Qp8bbJWiAxYAEBkWD7IQs0JsAKiYtvVhFAZ5ulGHI93RCA1PGjT56k0BIEdCHpneVSQsoAeZ/Isp57RiZj7gedvKiogTrHz+ZR6taWs=</Q><DP>A+PUm/cojMRSBKWYkgTPRp7D+6BwjEA1ugEyGoorOzNbsDwMsgtjJA+3GwvBMNS4qDyTx6hdxy5tdITAN9/zjZfdlc0m2o0TVdkTZzu0NabUiPCS4MPjN2BhWI095cyJ369ZjNokdgkiO7+rq09US3LTj897fjtEXktsAfPT1Dc=</DP><DQ>wGGD+Q+f5AWeqgm+bLdutrs051Ux42z8v0EjAqz2yFcD+j8B5CbQzsZznKcId/gZ4MRihh/U6Z8iZViVYs3J5/GYK8swD/glT/mN6t5butN0BCLCY+TVhKDLuMqjBhncZaBmIf38UnUf3MJKbhM3xXnCqfRbYos5eTdVQ/iYE9M=</DQ><InverseQ>CgR86ElcfiEzk5n1g0tm2BJwJOPWYiibU8CMpxJLuq29I6MBGhmtL4ognwPKLwdBYiAo9Hiq21R+01okgyIVUDjLgVEu+z0rCMyhBtSI4d976tXQ7frbk+ogC7DHoFS93U7mlb0rtjESCQnHhHIHUoASNzYpAEmvyUbZxKIf8EQ=</InverseQ><D>AVcWbnrz6um8DePT6IvyMVPDV0moeUFIleHCQkjYqnodhpBGhgA7ZKqMftkMTX+GpsqvLsTZNUPQZDc9VPNmJvfR46aM1V7QJng2Wp/HUGIcenTQKW7Tif4c7kxH3KeipJkEYKSXCqB4VU8M1F+Y6daQayNps9I1OX28tSf2XvmewxCv5Vbax7ULQoTigWOX/2ulB9unFH/m5UJdamZgWzUlGv8eDF1DtT7oUSLQQRKZ0WNijSOwUX0rYi/mNsmLLHAv2lTHOBbtLXMQUXT5qDSX08X7wA7GtQmGnSrgeoYFiqnEDk4AIhkEsKy1Tx43OTMbmOWj720YaQnkWf5lgQ==</D></RSAKeyValue>";

        private const string NV02_Public = @"<RSAKeyValue><Modulus>5Xd3SFq5HXwQ32pXDBDxZrzlbe5TgN87ucIORdWROGFPyX+jkjeYgampqarNhUGFRGTAhfQu94MAOu3zf4Yx7HYG5jQwRyTaDgplnot3HqMmmLn7lUa/rfWtDlDSMKkm3s0zqyXEnT/Hk5fpaIkMXVImU2I7gWxjMaGnKLOTrHZW/CdsEKUp6ZlriEXpHhybmIFKBhl3MwyBM28YWfnmQjtvjuTDnN5KuSpxO5ZNU5zpJ9XreR49eXva9ybcy2XLEwKT0vaVogRNc364IuwPPhrlf75wEwS54EI9KQT6xEa1QHUV4po9fxaDDkrbew7MrisKTD5NJwT4IRy+2PKRHQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        private const string NV02_Private = @"<RSAKeyValue><Modulus>5Xd3SFq5HXwQ32pXDBDxZrzlbe5TgN87ucIORdWROGFPyX+jkjeYgampqarNhUGFRGTAhfQu94MAOu3zf4Yx7HYG5jQwRyTaDgplnot3HqMmmLn7lUa/rfWtDlDSMKkm3s0zqyXEnT/Hk5fpaIkMXVImU2I7gWxjMaGnKLOTrHZW/CdsEKUp6ZlriEXpHhybmIFKBhl3MwyBM28YWfnmQjtvjuTDnN5KuSpxO5ZNU5zpJ9XreR49eXva9ybcy2XLEwKT0vaVogRNc364IuwPPhrlf75wEwS54EI9KQT6xEa1QHUV4po9fxaDDkrbew7MrisKTD5NJwT4IRy+2PKRHQ==</Modulus><Exponent>AQAB</Exponent><P>+ot6LxuZ6pbHh7soOdDaHZuncitlNPyCypZmc2tTk0xAjLUZtuNXtjbwTzS1hNskIjpm6xT7hL4pMgZJUGo75XiI05KbQ0CEtJtT742ssJu7o4hF1X9f2SnXYrnVSkpc3pQuhHTezNs4kQ0qwCvQ5q+lqrvt6NWxIa1xZdEt8/s=</P><Q>6naACXAsyYTlJK9qXPRmSY5nsCeWSNNbsRRy9gyAEyEwcJkbFMREeGf1SiLRYKuCoi0o9tjal0qNx6X6EFY/SgG+BvtS5VLX+z/tc8xizxSiHSa+uUsfnjvtx2ecU43DNqSua25/Woh3Ns8bfnMdVpgir9W42uDv4yvbcZfAa8c=</Q><DP>rLC+gwHPUTakSRXjYBZogMfs3nCyzZbOv7xy2VM8w+ZnB5U8KCcDKgEsGiwGgZBak40VqytHQfOgiW5z0g2nQz+Vb985TV2HGsWFUTShtwlgiBNBryqgchq47r+QGCixYmlYtsunViQ0FSayNMr+rkKbOddW4ImKEeBurPNFbUk=</DP><DQ>nMMCNE1GBK7QLkkCiof14/RMq2CsgKCgS7NqccxFzYSBSKd1jdr0FLUdMaY567KAs0ISg4jWDhWQ2g3FNisUQb0MesqK0D0lnx1M/AGJhv86rNb86hKqXzeecZZ61PqRrPVKkRXeHH/lhUXQEimRUhaCCqKQ5/dBLNhj7n0R8H0=</DQ><InverseQ>fketYCRHpmQfvA3yIpSs7ihf67ofX9KNMR8OSMqo0ShWbZdHEY624FEVPsU1veOjHaFvkVoJ1AZb2Cx9y7fcPU/Nker3hMms2wW7UDznnwfgJLyztEcfk1uxNh9gv/fDsdLzFZ5OJDHrin5yxnZqlYuOpXQfz6WYAB9JKqdt32g=</InverseQ><D>BjmxgMMFVqlxA76s0eT7x5/EUbEvjsV/p1B+dqKOqArr0TmnRGZBuvLRaYr6vGzEotBg8MFpLMnfDbo66vDHFfU1xrUPwj5btBnQG/cIqkoe1/Hjw28B8EREB6llqnBMX6sNhc0mgmWiknyClpVxQm7iH00A9EfugQAzusT0cq9OEE19olMuuwF7ReEBkOuYeI6cSR29EFXFTKlS70u+W5WKd/rOLVc3z/aiLMvGMydSY+0CgUbZsWeF/TQiv2ESC5i8NAVAgC9H9UNks3ZYZq4E+6yvUfpbjVsrUv9KfBWoLcyQ61XD+8ZTuc2qO8aeXVa8EcHetq/ff8lCLCXQ+Q==</D></RSAKeyValue>";

        public static void EnsureLocalKeyPair(string manv, int keySize = 2048)
        {
            if (CryptoHelper.HasLocalKeyPair(manv))
                return;

            if (manv == "NV01")
            {
                CryptoHelper.SavePublicKeyLocal(manv, NV01_Public);
                CryptoHelper.SavePrivateKeyLocal(manv, NV01_Private);
                return;
            }
            if (manv == "NV02")
            {
                CryptoHelper.SavePublicKeyLocal(manv, NV02_Public);
                CryptoHelper.SavePrivateKeyLocal(manv, NV02_Private);
                return;
            }

            var keys = CryptoHelper.GenerateRSAKeyPair(keySize);
            CryptoHelper.SavePublicKeyLocal(manv, keys.PublicKeyXml);
            CryptoHelper.SavePrivateKeyLocal(manv, keys.PrivateKeyXml);
        }

        public static void RegenerateLocalKeyPair(string manv, int keySize = 2048)
        {
            var keys = CryptoHelper.GenerateRSAKeyPair(keySize);
            CryptoHelper.SavePublicKeyLocal(manv, keys.PublicKeyXml);
            CryptoHelper.SavePrivateKeyLocal(manv, keys.PrivateKeyXml);
        }

        public static void GenerateAndSaveLocal(string manv, int keySize = 2048)
        {
            var keys = CryptoHelper.GenerateRSAKeyPair(keySize);
            CryptoHelper.SavePublicKeyLocal(manv, keys.PublicKeyXml);
            CryptoHelper.SavePrivateKeyLocal(manv, keys.PrivateKeyXml);
        }
    }
}
