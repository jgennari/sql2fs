# sql2fs
A tool for creating a file-system representation of a SQL Server database.

    Usage:
    sql2fs [options]

    Options:
    -s, --server <server>                   Required: Connect to a SQL Server instance
    -d, --database <database>               Optional: Connect to a specific database
    -dir, --directory <directory>           Optional: Directory to store the files
    -u, --username <username>               Optional: Username to connect connect to the server
    -p, --password <password>               Optional: Password to connect to the server
    -t, --types <types>                     Types of documentation to include (all, t = table, s = stored procedure, v = view, u = user-defined function) 
                                            [default: all]
    -c, --clean                             Clean the provided directory before saving documentation [default: False]
    --prune                                 Remove any existing file if the object doesn't exist [default: True]
    -e, --ignore-encryption                 Ignore any encrypted object [default: True]
    -ni, --name-include <name-include>      Comma-seperated list of object name prefixes to include
    -ne, --name-exclude <name-exclude>      Comma-seperated list of object name prefixes to exclude
    -si, --schema-include <schema-include>  Comma-seperated list of object schemas to include
    -se, --schema-exclude <schema-exclude>  Comma-seperated list of object schemas to exclude
    --version                               Show version information
    -?, -h, --help                          Show help and usage information