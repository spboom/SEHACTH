SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='TRADITIONAL,ALLOW_INVALID_DATES';

-- -----------------------------------------------------
-- Schema mydb
-- -----------------------------------------------------
-- -----------------------------------------------------
-- Schema Server
-- -----------------------------------------------------
DROP SCHEMA IF EXISTS `Server` ;
CREATE SCHEMA IF NOT EXISTS `Server` DEFAULT CHARACTER SET latin1 ;
USE `Server` ;

-- -----------------------------------------------------
-- Table `Server`.`Role`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `Server`.`Role` ;

CREATE TABLE IF NOT EXISTS `Server`.`Role` (
  `Id` INT NOT NULL AUTO_INCREMENT,
  `Name` VARCHAR(45) NOT NULL,
  PRIMARY KEY (`Id`))
ENGINE = InnoDB;


-- -----------------------------------------------------
-- Table `Server`.`User`
-- -----------------------------------------------------
DROP TABLE IF EXISTS `Server`.`User` ;

CREATE TABLE IF NOT EXISTS `Server`.`User` (
  `UserName` VARCHAR(45) NOT NULL,
  `Password` VARCHAR(100) NOT NULL,
  `FirstName` VARCHAR(45) NOT NULL,
  `MiddleName` VARCHAR(45) NULL,
  `LastName` VARCHAR(45) NOT NULL,
  `Role_Id` INT NOT NULL,
  PRIMARY KEY (`UserName`, `Role_Id`),
  INDEX `fk_User_Role_idx` (`Role_Id` ASC),
  CONSTRAINT `fk_User_Role`
    FOREIGN KEY (`Role_Id`)
    REFERENCES `Server`.`Role` (`Id`)
    ON DELETE NO ACTION
    ON UPDATE NO ACTION)
ENGINE = InnoDB;


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
