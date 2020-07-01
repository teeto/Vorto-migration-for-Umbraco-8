# Vorto-migration-for-Umbraco-8
A health check that helps migrating vorto properties to Umbraco 8

After updating to Umbraco v8 from v7, all the Vorto properties become label properties.
This healtcheck looks all the content items for vorto xml and stores it as string language variant.
It uses the Vorto models to serialize the xml.

First thing after updating to Umbraco 8 is change all document types that have Vorto properties to allow changing variant by culture.
Then on each document type mark each vorto property as changing by culture.
As all the Vorto data types have become labels, change them to umbraco 8 compatible string data types: textbox, textarea or richtext editor.
Then on settings go to health checks, an run the Vorto health check.
If a document type with vorto properties is not allowed to change by culture it shows an error.
All the errors are in spanish language :)
