# PaperlessLoader (pll)

`pll` can upload documents to a Paperless-ngx server including tags. The tags can be also created by `pll`.

There is also a functionality to read the tags assigned to a file (works only on `macOS`).

`pll` requires a config file called `config.yml` located in the config folder of the respective OS. On Linux `~/.config/pll` or on Windows in `appdata` folder. There is an example file in this repository. 

## Profile Config

```yml
profiles:
  - name: MyProfile
    append_string: My Append - String
    input_date_regex: \d{4}-(?:0[1-9]|1[0-2])-(?:0[1-9]|[1-2]\d|3[01])
    input_date_format: yyyy-MM # Default is yyyy-MM-dd
    output_date_format: yyyy-MM # Default is yyyy-MM-dd
    tags:
      - 01-MyTag01
      - 02-MyTag02
      - 03-MyTag03
```

## Usage

### Import Documents

```bash
pll document import path/to/my/documents

pll document import path/to/my/documents --includeMacOsTags
```

Not existing tags will be automatically created during import.

### Import Documents using a Profile

Below command also renames the file based on settings in the Profile

```bash
pll document import-with-profile -r -p MyProfile "~/Documents/pll Imports/MyDocs"
```

### Tag Management

```bash
pll tags list

1: MyTag1
2: MyTag2
3: MyTag3

pll tags add MyTag4

4: MyTag4
```
