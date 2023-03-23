DROP SCHEMA public CASCADE;
CREATE SCHEMA public

CREATE TABLE public.categories (
	ctg_id int NOT NULL GENERATED ALWAYS AS IDENTITY,
	ctg_name varchar(8) NOT NULL,
	PRIMARY KEY(ctg_id)
);

CREATE TABLE public.files (
	f_id int NOT NULL GENERATED ALWAYS AS IDENTITY,
	f_name varchar(24) NOT NULL,
	ctg_id int REFERENCES public.categories(ctg_id) ON DELETE CASCADE ON UPDATE CASCADE,
	PRIMARY KEY(f_id)
);

CREATE TABLE public.substations (
	sbst_id int NOT NULL GENERATED ALWAYS AS IDENTITY,
	sbst_name varchar(40) NOT NULL,
	PRIMARY KEY(sbst_id)
);

CREATE TABLE public.transformers (
	trans_id int NOT NULL GENERATED ALWAYS AS IDENTITY,
	trans_name varchar(24) NOT NULL,
	sbst_id int REFERENCES public.substations(sbst_id) ON DELETE CASCADE ON UPDATE CASCADE,
	PRIMARY KEY(trans_id)
);

CREATE TABLE public.parameters (
	param_id int NOT NULL GENERATED ALWAYS AS IDENTITY,
	param_name varchar(64) NOT NULL,
	PRIMARY KEY(param_id)
);

CREATE TABLE public.fields (
	field_id int NOT NULL GENERATED ALWAYS AS IDENTITY,
	field_name varchar(48) NOT NULL,
	field_desc varchar(128),
	param_id int REFERENCES public.parameters(param_id),
	f_id int REFERENCES public.files(f_id) ON DELETE CASCADE ON UPDATE CASCADE,
	PRIMARY KEY(field_id)
);

CREATE TABLE public.uploaded_files (
	uf_id int NOT NULL GENERATED ALWAYS AS IDENTITY,
	uf_name varchar(48) NOT NULL,	
	datetime timestamp without time zone,
	PRIMARY KEY(uf_id)
);

CREATE TABLE public.measurements (
	datetime timestamp without time zone NOT NULL,
	value real NOT NULL,
	value_min real,
	value_max real,
	field_id int REFERENCES public.fields(field_id) ON DELETE CASCADE ON UPDATE CASCADE,
	trans_id int REFERENCES public.transformers(trans_id) ON DELETE CASCADE ON UPDATE CASCADE,
	phase varchar(1),
	uf_id int REFERENCES public.uploaded_files(uf_id) ON DELETE CASCADE ON UPDATE CASCADE,
	avg_range SMALLINT,
	PRIMARY KEY(datetime, field_id, trans_id, phase, avg_range)
);

CREATE TABLE public.temp_measurements (
	datetime timestamp without time zone,
	value real NOT null,
	value_min real,
	value_max real,
	field_id int,
	trans_id int,
	phase varchar(1),
	avg_range SMALLINT,
	PRIMARY KEY(datetime, field_id, trans_id, phase, avg_range)
);

CREATE TABLE public.preset_names (
	preset_id int NOT NULL GENERATED ALWAYS AS IDENTITY,
	preset_name varchar(40) NOT NULL,
	datetime timestamp without time zone,
	PRIMARY KEY(preset_id)
);

CREATE TABLE public.fields_presets (
	field_record_id int NOT NULL GENERATED ALWAYS AS IDENTITY,
	preset_id int REFERENCES public.preset_names(preset_id) ON DELETE CASCADE ON UPDATE CASCADE,
	field_id int REFERENCES public.fields(field_id) ON DELETE CASCADE ON UPDATE CASCADE,
	checked_value boolean,
	checked_value_min boolean,
	checked_value_max boolean,
	value_order smallint,
	value_min_order smallint,
	value_max_order smallint,
	PRIMARY KEY(field_record_id)
);

insert into public.categories (ctg_name) values ('АСУОТ');
insert into public.substations (sbst_name) values ('ПС 500 кВ Хаб.');
insert into public.transformers (trans_name, sbst_id) values ('АТ-1', 1);
insert into public.files (f_name, ctg_id) values ('X_Log_General', 1);
--*/