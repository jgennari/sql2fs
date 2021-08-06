# sql2fs
A tool for creating a file-system representation of a SQL Server database.

    Usage:
    sql2fs [options]

    Options:
    -s, --server <server>                   SQL Server conection (IP address, named instance, machine name)
    -dir, --directory <directory>           Directory to store the files
    -d, --database <database>               Specific database to connect to, otherwise first database
    -t, --types <types>                     Types of documentation to include (all, t = table, s = stored procedure, v = view, u = user-defined function) 
                                            [default: all]
    -c, --clean                             Clean the provided directory before saving documentation [default: False]
    -p, --prune                             Remove any existing file if the object doesn't exist [default: True]
    -e, --ignore-encryption                 Ignore any encrypted object [default: True]
    -ni, --name-include <name-include>      Comma-seperated list of object name prefixes to include
    -ne, --name-exclude <name-exclude>      Comma-seperated list of object name prefixes to exclude
    -si, --schema-include <schema-include>  Comma-seperated list of object schemas to include
    -se, --schema-exclude <schema-exclude>  Comma-seperated list of object schemas to exclude
    --version                               Show version information
    -?, -h, --help                          Show help and usage information