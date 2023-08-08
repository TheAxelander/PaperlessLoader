# PaperlessLoader (pll)

`pll` can upload documents to a Paperless-ngx server including tags. The tags can be also created by `pll`.

There is also a functionality to read the tags assigned to a file (works only on `macOS`).

`pll` requires a config file called `config.yml` located in the config folder of the respective OS. On Linux `~/.config/pll` or on Windows in `appdata` folder. There is an example file in this repository. 

## Usage

### Import Documents

```bash
pll document import path/to/my/documents

pll document import path/to/my/documents --includeMacOsTags
```

Not existing tags will be automatically created during import.

### Tag Management

```bash
pll tags list

1: MyTag1
2: MyTag2
3: MyTag3

pll tags add MyTag4

4: MyTag4
```
